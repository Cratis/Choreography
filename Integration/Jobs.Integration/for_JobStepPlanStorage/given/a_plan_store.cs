// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.MongoDB;
using Cratis.Orleans.Storage.MongoDB.Jobs;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Orleans.Jobs.Integration.for_JobStepPlanStorage.given;

public class a_plan_store : Specification
{
    protected JobId _jobId = JobId.New();
    protected Guid _incarnation = Guid.NewGuid();
    protected JobStepPlanStorage _store;
    protected JobStepPlanStorage _faultingStore;
    protected IMongoCollection<BsonDocument> _collection;
    protected List<PlannedJobStep> _plan = [];
    protected int _inserts;
    protected int _deletes;
    protected Func<int, Task>? _beforeInsert;
    protected Func<Task>? _beforeDelete;
    protected Func<int, Task>? _afterInsert;

    void Establish()
    {
        _ = JobsClusterFixture.Shared;
        JobsClusterFixture.ResetStorage();
        var database = new MongoClient(JobsClusterFixture.ConnectionString).GetDatabase(JobsClusterFixture.DatabaseName);
        _collection = database.GetCollection<BsonDocument>(WellKnownCollectionNames.JobStepPlans);
        var retry = new TransientRetry(1, TimeSpan.Zero);
        _store = new(database, retry);

        // Only the fault boundary is substituted: every acknowledged write and every read reaches real MongoDB.
        var collection = Substitute.For<IMongoCollection<BsonDocument>>();
        collection.CollectionNamespace.Returns(_collection.CollectionNamespace);
        collection.Indexes.Returns(_collection.Indexes);
        collection.FindAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<FindOptions<BsonDocument, BsonDocument>>(), Arg.Any<CancellationToken>())
            .Returns(call => _collection.FindAsync(call.Arg<FilterDefinition<BsonDocument>>(), call.Arg<FindOptions<BsonDocument, BsonDocument>>(), call.Arg<CancellationToken>()));
        collection.AggregateAsync(Arg.Any<PipelineDefinition<BsonDocument, BsonDocument>>(), Arg.Any<AggregateOptions>(), Arg.Any<CancellationToken>())
            .Returns(call => _collection.AggregateAsync(call.Arg<PipelineDefinition<BsonDocument, BsonDocument>>(), call.Arg<AggregateOptions>(), call.Arg<CancellationToken>()));
        collection.DeleteManyAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            _deletes++;
            if (_beforeDelete is not null)
            {
                await _beforeDelete();
            }
            return await _collection.DeleteManyAsync(call.Arg<FilterDefinition<BsonDocument>>(), call.Arg<CancellationToken>());
        });
        collection.InsertManyAsync(Arg.Any<IEnumerable<BsonDocument>>(), Arg.Any<InsertManyOptions>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            var attempt = ++_inserts;
            if (_beforeInsert is not null)
            {
                await _beforeInsert(attempt);
            }
            await _collection.InsertManyAsync(call.Arg<IEnumerable<BsonDocument>>(), call.Arg<InsertManyOptions>(), call.Arg<CancellationToken>());
            if (_afterInsert is not null)
            {
                await _afterInsert(attempt);
            }
        });
        var faultingDatabase = Substitute.For<IMongoDatabase>();
        faultingDatabase.GetCollection<BsonDocument>(WellKnownCollectionNames.JobStepPlans).Returns(collection);
        _faultingStore = new(faultingDatabase, retry);
    }

    protected void LargePlan() => _plan = Enumerable.Range(0, 20)
        .Select(index => new PlannedJobStep(JobStepId.New(), new JobStepType("step"), new(index), "request", new string('x', 1024 * 1024)))
        .ToList();

    protected async Task AssertStoredPlan()
    {
        var read = await _store.GetForJob(_jobId, _incarnation);
        read.AsT0.ShouldEqual(_plan);
        (await _collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).ShouldEqual(_plan.Count);
    }
}
