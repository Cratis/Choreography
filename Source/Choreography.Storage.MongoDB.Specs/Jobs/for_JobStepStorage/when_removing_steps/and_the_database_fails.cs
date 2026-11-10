// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using MongoDB.Driver;
using Catch = Cratis.Monads.Catch;

namespace Cratis.Choreography.Storage.MongoDB.Jobs.for_JobStepStorage.when_removing_steps;

public class and_the_database_fails : Jobs.given.a_database
{
    JobStepStorage _storage;
    Catch _removeAll;
    Catch _removeNonFailed;
    Catch _removeOne;

    void Establish()
    {
        _storage = new JobStepStorage(_database);
        var collection = Collection<JobStepState>(WellKnownCollectionNames.JobSteps);
        Collection<JobStepState>(WellKnownCollectionNames.FailedJobSteps);
        collection.DeleteManyAsync(Arg.Any<FilterDefinition<JobStepState>>(), Arg.Any<CancellationToken>()).Returns<Task<DeleteResult>>(_ => throw new InvalidOperationException("boom"));
        collection.DeleteOneAsync(Arg.Any<FilterDefinition<JobStepState>>(), Arg.Any<CancellationToken>()).Returns<Task<DeleteResult>>(_ => throw new InvalidOperationException("boom"));
    }

    async Task Because()
    {
        var jobId = JobId.New();
        _removeAll = await _storage.RemoveAllForJob(jobId);
        _removeNonFailed = await _storage.RemoveAllNonFailedForJob(jobId);
        _removeOne = await _storage.Remove(jobId, JobStepId.New());
    }

    [Fact] void should_report_failure_removing_all() => _removeAll.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_failure_removing_non_failed() => _removeNonFailed.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_failure_removing_one() => _removeOne.IsSuccess.ShouldBeFalse();
}
