// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Jobs.Stages;
using Cratis.Orleans.Storage.Jobs;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Orleans.Storage.MongoDB.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobStepPlanStorage"/> for MongoDB, keeping one small document per planned step.
/// </summary>
/// <param name="database"><see cref="IMongoDatabase"/> for persistence.</param>
/// <param name="retry">The <see cref="TransientRetry"/> applied to storage operations.</param>
public class JobStepPlanStorage(IMongoDatabase database, TransientRetry retry) : IJobStepPlanStorage
{
    const string JobIdField = "jobId";
    const string IndexField = "index";

    readonly ConcurrentDictionary<string, byte> _ensuredIndexes = new();

    IMongoCollection<BsonDocument> Collection => database.GetCollection<BsonDocument>(WellKnownCollectionNames.JobStepPlans);

    /// <inheritdoc/>
    public async Task<Catch> Save(JobId jobId, IReadOnlyList<PlannedJobStep> plan)
    {
        try
        {
            await EnsureIndexes().ConfigureAwait(false);
            await retry.Execute($"saving the step plan of job {jobId}", async () =>
            {
                await Collection.DeleteManyAsync(JobFilter(jobId)).ConfigureAwait(false);
                if (plan.Count > 0)
                {
                    foreach (var batch in Batches(jobId, plan))
                    {
                        await Collection.InsertManyAsync(batch, new InsertManyOptions { IsOrdered = false }).ConfigureAwait(false);
                    }
                }
                return true;
            }).ConfigureAwait(false);
            return Catch.Success();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public async Task<Catch<IReadOnlyList<PlannedJobStep>>> GetForJob(JobId jobId)
    {
        try
        {
            await EnsureIndexes().ConfigureAwait(false);
            var documents = await retry.Execute($"getting the step plan of job {jobId}", async () =>
            {
                using var cursor = await Collection.FindAsync(JobFilter(jobId), new FindOptions<BsonDocument> { Sort = Builders<BsonDocument>.Sort.Ascending(IndexField) }).ConfigureAwait(false);
                return await cursor.ToListAsync().ConfigureAwait(false);
            }).ConfigureAwait(false);
            return Catch<IReadOnlyList<PlannedJobStep>>.Success([.. documents.Select(FromDocument)]);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public async Task<Catch<IReadOnlyList<JobId>>> GetJobsWithPlans()
    {
        try
        {
            await EnsureIndexes().ConfigureAwait(false);
            var ids = await retry.Execute("getting the jobs that have a step plan", async () =>
            {
                using var cursor = await Collection.DistinctAsync<BsonBinaryData>(JobIdField, FilterDefinition<BsonDocument>.Empty).ConfigureAwait(false);
                return await cursor.ToListAsync().ConfigureAwait(false);
            }).ConfigureAwait(false);
            return Catch<IReadOnlyList<JobId>>.Success([.. ids.Select(id => new JobId(id.AsGuid))]);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public async Task<Catch> RemoveForJob(JobId jobId)
    {
        try
        {
            await EnsureIndexes().ConfigureAwait(false);
            await Collection.DeleteManyAsync(JobFilter(jobId)).ConfigureAwait(false);
            return Catch.Success();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    static FilterDefinition<BsonDocument> JobFilter(JobId jobId) =>
        Builders<BsonDocument>.Filter.Eq(JobIdField, new BsonBinaryData(jobId.Value, GuidRepresentation.Standard));

    static IEnumerable<List<BsonDocument>> Batches(JobId jobId, IReadOnlyList<PlannedJobStep> plan)
    {
        // Leave room for command overhead; individual documents remain subject to MongoDB's 16 MiB limit.
        const int maxBytes = 15 * 1024 * 1024;
        var batch = new List<BsonDocument>();
        var bytes = 0;
        for (var index = 0; index < plan.Count; index++)
        {
            var document = ToDocument(jobId, index, plan[index]);
            var size = document.ToBson().Length;
            if (batch.Count > 0 && bytes + size > maxBytes)
            {
                yield return batch;
                batch = [];
                bytes = 0;
            }
            batch.Add(document);
            bytes += size;
        }
        if (batch.Count > 0)
        {
            yield return batch;
        }
    }

    static BsonDocument ToDocument(JobId jobId, int index, PlannedJobStep step) => new()
    {
        { "_id", $"{jobId.Value:N}-{index:D10}" },
        { JobIdField, new BsonBinaryData(jobId.Value, GuidRepresentation.Standard) },
        { IndexField, index },
        { "jobStepId", new BsonBinaryData(step.Id.Value, GuidRepresentation.Standard) },
        { "type", step.Type.Value },
        { "stage", step.Stage.Value },
        { "requestType", step.RequestType },
        { "requestJson", step.RequestJson }
    };

    static PlannedJobStep FromDocument(BsonDocument document) => new(
        new JobStepId(document["jobStepId"].AsBsonBinaryData.ToGuid()),
        new JobStepType(document["type"].AsString),
        new JobStepStage(document["stage"].AsInt32),
        document["requestType"].AsString,
        document["requestJson"].AsString);

    /// <summary>
    /// Ensures the compound index idempotently, leaving any legacy jobId-only index in place.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    async Task EnsureIndexes() =>
        await Collection.EnsureIndexesOnceAsync(
            _ensuredIndexes,
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending(JobIdField).Ascending(IndexField),
                new CreateIndexOptions { Name = "jobId_index", Background = true })).ConfigureAwait(false);
}
