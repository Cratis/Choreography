// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Catch = Cratis.Monads.Catch;

namespace Cratis.Choreography.Storage.MongoDB.Jobs.for_JobStepStorage.when_removing_steps;

public class for_a_job : Jobs.given.a_database
{
    JobStepStorage _storage;
    IMongoCollection<JobStepState> _collection;
    IMongoCollection<JobStepState> _failedCollection;
    List<(string Operation, BsonDocument Filter)> _calls;
    JobId _jobId;
    JobStepId _jobStepId;
    Catch _allResult;
    Catch _nonFailedResult;
    Catch _oneResult;
    List<(string Operation, BsonDocument Filter)> _nonFailedCalls;
    List<(string Operation, BsonDocument Filter)> _allCalls;
    List<(string Operation, BsonDocument Filter)> _oneCalls;

    void Establish()
    {
        _calls = [];
        _jobId = JobId.New();
        _jobStepId = JobStepId.New();
        _storage = new JobStepStorage(_database);
        _collection = Collection<JobStepState>(WellKnownCollectionNames.JobSteps);
        _failedCollection = Collection<JobStepState>(WellKnownCollectionNames.FailedJobSteps);
        Record(_collection, "steps");
        Record(_failedCollection, "failed");
    }

    async Task Because()
    {
        _nonFailedResult = await _storage.RemoveAllNonFailedForJob(_jobId);
        _nonFailedCalls = Take();

        _allResult = await _storage.RemoveAllForJob(_jobId);
        _allCalls = Take();

        _oneResult = await _storage.Remove(_jobId, _jobStepId);
        _oneCalls = Take();
    }

    [Fact] void should_succeed_removing_non_failed() => _nonFailedResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_succeed_removing_all() => _allResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_succeed_removing_one() => _oneResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_only_delete_many_from_the_steps_collection_when_removing_non_failed() => _nonFailedCalls.Select(_ => _.Operation).ShouldEqual(["steps:many"]);
    [Fact] void should_match_the_job_when_removing_non_failed() => _nonFailedCalls[0].Filter.ShouldEqual(JobFilter());
    [Fact] void should_delete_many_from_the_steps_collection_then_the_failed_collection_when_removing_all() => _allCalls.Select(_ => _.Operation).ShouldEqual(["steps:many", "failed:many"]);
    [Fact] void should_match_the_job_in_both_collections_when_removing_all() => _allCalls.Select(_ => _.Filter).ShouldEqual([JobFilter(), JobFilter()]);
    [Fact] void should_delete_one_from_the_steps_collection_then_the_failed_collection() => _oneCalls.Select(_ => _.Operation).ShouldEqual(["steps:one", "failed:one"]);
    [Fact] void should_match_the_job_and_step_in_both_collections() => _oneCalls.Select(_ => _.Filter).ShouldEqual([StepFilter(), StepFilter()]);

    static BsonDocument Render(FilterDefinition<JobStepState> filter) =>
        filter.Render(new RenderArgs<JobStepState>(BsonSerializer.LookupSerializer<JobStepState>(), BsonSerializer.SerializerRegistry));

    BsonDocument JobFilter() => new("_id.jobId", new BsonBinaryData(_jobId.Value, GuidRepresentation.Standard));

    BsonDocument StepFilter() => new("_id", new BsonDocument
    {
        { "jobId", new BsonBinaryData(_jobId.Value, GuidRepresentation.Standard) },
        { "jobStepId", new BsonBinaryData(_jobStepId.Value, GuidRepresentation.Standard) }
    });

    void Record(IMongoCollection<JobStepState> collection, string name)
    {
        collection
            .When(_ => _.DeleteManyAsync(Arg.Any<FilterDefinition<JobStepState>>(), Arg.Any<CancellationToken>()))
            .Do(call => _calls.Add(($"{name}:many", Render(call.Arg<FilterDefinition<JobStepState>>()))));
        collection
            .When(_ => _.DeleteOneAsync(Arg.Any<FilterDefinition<JobStepState>>(), Arg.Any<CancellationToken>()))
            .Do(call => _calls.Add(($"{name}:one", Render(call.Arg<FilterDefinition<JobStepState>>()))));
    }

    List<(string Operation, BsonDocument Filter)> Take()
    {
        var taken = _calls.ToList();
        _calls.Clear();
        return taken;
    }
}
