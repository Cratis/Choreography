// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Jobs.Stages;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Monads;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Choreography.Storage.MongoDB.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobStepPlanStorage"/> for MongoDB, keeping one small document per planned step.
/// </summary>
/// <param name="database"><see cref="IMongoDatabase"/> for persistence.</param>
/// <param name="retry">The <see cref="TransientRetry"/> applied to storage operations.</param>
public class JobStepPlanStorage(IMongoDatabase database, TransientRetry retry) : IJobStepPlanStorage
{
    const string JobIdField = "jobId";
    const string IndexField = "index";
    const string IncarnationField = "incarnation";

    readonly ConcurrentDictionary<string, byte> _ensuredIndexes = new();

    IMongoCollection<BsonDocument> Collection => database.GetCollection<BsonDocument>(WellKnownCollectionNames.JobStepPlans);

    /// <inheritdoc/>
    public async Task<Catch> Save(JobId jobId, Guid incarnation, IReadOnlyList<PlannedJobStep> plan)
    {
        try
        {
            await EnsureIndexes().ConfigureAwait(false);
            await retry.Execute($"saving the step plan of job {jobId}", async () =>
            {
                await Collection.DeleteManyAsync(PlanFilter(jobId, incarnation)).ConfigureAwait(false);
                if (plan.Count > 0)
                {
                    foreach (var batch in Batches(jobId, incarnation, plan))
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
    public async Task<Catch<IReadOnlyList<PlannedJobStep>>> GetForJob(JobId jobId, Guid incarnation)
    {
        try
        {
            await EnsureIndexes().ConfigureAwait(false);
            var documents = await retry.Execute($"getting the step plan of job {jobId}", async () =>
            {
                using var cursor = await Collection.FindAsync(PlanFilter(jobId, incarnation), new FindOptions<BsonDocument> { Sort = Builders<BsonDocument>.Sort.Ascending(IndexField) }).ConfigureAwait(false);
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
    public async Task<Catch<IReadOnlyList<JobStepPlanKey>>> GetJobsWithPlans()
    {
        try
        {
            await EnsureIndexes().ConfigureAwait(false);
            var ids = await retry.Execute("getting the jobs that have a step plan", async () =>
            {
                // Read identities from the orphan documents, never from a possibly replaced job state.
                var pipeline = new[]
                {
                    new BsonDocument("$match", new BsonDocument(IncarnationField, new BsonDocument("$exists", true))),
                    new BsonDocument("$group", new BsonDocument("_id", new BsonDocument { { JobIdField, "$" + JobIdField }, { IncarnationField, "$" + IncarnationField } }))
                };
                using var cursor = await Collection.AggregateAsync<BsonDocument>(pipeline).ConfigureAwait(false);
                return await cursor.ToListAsync().ConfigureAwait(false);
            }).ConfigureAwait(false);
            return Catch<IReadOnlyList<JobStepPlanKey>>.Success([.. ids.Select(document => new JobStepPlanKey(
                new JobId(document["_id"][JobIdField].AsBsonBinaryData.ToGuid()), document["_id"][IncarnationField].AsBsonBinaryData.ToGuid())).Distinct()]);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public async Task<Catch> RemoveForJob(JobId jobId, Guid incarnation)
    {
        try
        {
            await EnsureIndexes().ConfigureAwait(false);
            await Collection.DeleteManyAsync(PlanFilter(jobId, incarnation)).ConfigureAwait(false);
            return Catch.Success();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    static FilterDefinition<BsonDocument> PlanFilter(JobId jobId, Guid incarnation) =>
        Builders<BsonDocument>.Filter.Eq(JobIdField, new BsonBinaryData(jobId.Value, GuidRepresentation.Standard)) &
        Builders<BsonDocument>.Filter.Eq(IncarnationField, new BsonBinaryData(incarnation, GuidRepresentation.Standard));

    static IEnumerable<List<BsonDocument>> Batches(JobId jobId, Guid incarnation, IReadOnlyList<PlannedJobStep> plan)
    {
        // Leave room for command overhead; individual documents remain subject to MongoDB's 16 MiB limit.
        const int maxBytes = 15 * 1024 * 1024;
        var batch = new List<BsonDocument>();
        var bytes = 0;
        for (var index = 0; index < plan.Count; index++)
        {
            var document = ToDocument(jobId, incarnation, index, plan[index]);
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

    static BsonDocument ToDocument(JobId jobId, Guid incarnation, int index, PlannedJobStep step) => new()
    {
        { "_id", $"{jobId.Value:N}-{incarnation:N}-{index:D10}" },
        { IncarnationField, new BsonBinaryData(incarnation, GuidRepresentation.Standard) },
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
                Builders<BsonDocument>.IndexKeys.Ascending(JobIdField).Ascending(IncarnationField).Ascending(IndexField),
                new CreateIndexOptions { Name = "jobId_incarnation_index", Background = true })).ConfigureAwait(false);
}
