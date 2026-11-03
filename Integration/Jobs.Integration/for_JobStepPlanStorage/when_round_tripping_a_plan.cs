// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Orleans.Jobs.Integration.for_JobStepPlanStorage;

[Collection(JobsClusterCollection.Name)]
public class when_round_tripping_a_plan : given.a_plan_store
{
    async Task Establish()
    {
        _plan = Enumerable.Range(0, 4)
            .Select(index => new PlannedJobStep(JobStepId.New(), new JobStepType("step"), new(index), "request", $"{{\"index\":{index}}}"))
            .ToList();
        (await _store.Save(_jobId, _plan)).RethrowError();

        // Reverse physical insertion order so an unsorted read cannot accidentally satisfy the order assertion.
        var documents = await _collection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        await _collection.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
        await _collection.InsertManyAsync(documents.AsEnumerable().Reverse());
    }

    [Fact] Task should_get_the_plan_in_index_order() => AssertStoredPlan();
    [Fact]
    async Task should_store_standard_guid_binary_data()
    {
        var document = await _collection.Find(FilterDefinition<BsonDocument>.Empty).FirstAsync(TestContext.Current.CancellationToken);
        document["jobId"].AsBsonBinaryData.SubType.ShouldEqual(BsonBinarySubType.UuidStandard);
        document["jobId"].AsBsonBinaryData.ToGuid().ShouldEqual(_jobId.Value);
        _plan.Select(step => step.Id.Value).ShouldContain(document["jobStepId"].AsBsonBinaryData.ToGuid());
    }
    [Fact] async Task should_return_empty_for_a_missing_plan() => (await _store.GetForJob(JobId.New())).AsT0.ShouldBeEmpty();
    [Fact]
    async Task should_represent_an_empty_plan_as_missing()
    {
        (await _store.Save(_jobId, [])).RethrowError();
        (await _store.GetForJob(_jobId)).AsT0.ShouldBeEmpty();
        (await _store.GetJobsWithPlans()).AsT0.ShouldBeEmpty();
    }
    [Fact]
    async Task should_ensure_the_compound_index_idempotently()
    {
        (await _store.Save(_jobId, _plan)).RethrowError();
        using var cursor = await _collection.Indexes.ListAsync(cancellationToken: TestContext.Current.CancellationToken);
        var indexes = await cursor.ToListAsync(TestContext.Current.CancellationToken);
        indexes.Single(index => index["name"] == "jobId_index")["key"].AsBsonDocument
            .ShouldEqual(new BsonDocument { ["jobId"] = 1, ["index"] = 1 });
    }
}
