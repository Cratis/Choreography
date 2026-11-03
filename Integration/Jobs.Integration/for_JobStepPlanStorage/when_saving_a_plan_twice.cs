// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Choreography.Jobs.Integration.for_JobStepPlanStorage;

[Collection(JobsClusterCollection.Name)]
public class when_saving_a_plan_twice : given.a_plan_store
{
    List<BsonDocument> _indexes = [];

    async Task Establish()
    {
        _plan = Enumerable.Range(0, 4)
            .Select(index => new PlannedJobStep(JobStepId.New(), new JobStepType("step"), new(index), "request", $"{{\"index\":{index}}}"))
            .ToList();
        (await _store.Save(_jobId, _incarnation, _plan)).RethrowError();
    }

    async Task Because()
    {
        (await _store.Save(_jobId, _incarnation, _plan)).RethrowError();
        using var cursor = await _collection.Indexes.ListAsync();
        _indexes = await cursor.ToListAsync();
    }

    [Fact]
    void should_ensure_the_compound_index_idempotently() =>
        _indexes.Single(index => index["name"] == "jobId_incarnation_index")["key"].AsBsonDocument
            .ShouldEqual(new BsonDocument { ["jobId"] = 1, ["incarnation"] = 1, ["index"] = 1 });

    [Fact] Task should_keep_a_single_copy_of_the_plan() => AssertStoredPlan();
}
