// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using MongoDB.Driver;
using Catch = Cratis.Monads.Catch;

namespace Cratis.Choreography.Storage.MongoDB.Jobs.for_JobStepStorage.when_removing_steps;

public class for_a_job : Jobs.given.a_database
{
    JobStepStorage _storage;
    IMongoCollection<JobStepState> _collection;
    IMongoCollection<JobStepState> _failedCollection;
    Catch _allResult;
    Catch _nonFailedResult;
    Catch _oneResult;

    void Establish()
    {
        _storage = new JobStepStorage(_database);
        _collection = Collection<JobStepState>(WellKnownCollectionNames.JobSteps);
        _failedCollection = Collection<JobStepState>(WellKnownCollectionNames.FailedJobSteps);
    }

    async Task Because()
    {
        var jobId = JobId.New();
        _nonFailedResult = await _storage.RemoveAllNonFailedForJob(jobId);
        _collection.ClearReceivedCalls();
        _failedCollection.ClearReceivedCalls();

        _allResult = await _storage.RemoveAllForJob(jobId);
        _oneResult = await _storage.Remove(jobId, JobStepId.New());
    }

    [Fact] void should_succeed_removing_non_failed() => _nonFailedResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_succeed_removing_all() => _allResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_succeed_removing_one() => _oneResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_delete_many_from_the_steps_collection() => _collection.Received(1).DeleteManyAsync(Arg.Any<FilterDefinition<JobStepState>>(), Arg.Any<CancellationToken>());
    [Fact] void should_delete_many_from_the_failed_collection() => _failedCollection.Received(1).DeleteManyAsync(Arg.Any<FilterDefinition<JobStepState>>(), Arg.Any<CancellationToken>());
    [Fact] void should_delete_one_from_the_steps_collection() => _collection.Received(1).DeleteOneAsync(Arg.Any<FilterDefinition<JobStepState>>(), Arg.Any<CancellationToken>());
    [Fact] void should_delete_one_from_the_failed_collection() => _failedCollection.Received(1).DeleteOneAsync(Arg.Any<FilterDefinition<JobStepState>>(), Arg.Any<CancellationToken>());
}
