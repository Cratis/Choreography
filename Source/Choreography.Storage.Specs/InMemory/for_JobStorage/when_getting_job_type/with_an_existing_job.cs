// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Monads;
using JobError = Cratis.Choreography.Storage.Jobs.JobError;

namespace Cratis.Choreography.Storage.InMemory.for_JobStorage.when_getting_job_type;

public class with_an_existing_job : given.jobs_in_storage
{
    Catch<JobType, JobError> _result;
    Task Establish() => _storage.Save(_id, new JobState { Id = _id, Type = _type });
    async Task Because() => _result = await _storage.GetJobType(_id);

    [Fact] void should_return_the_stored_type() => _result.AsT0.ShouldEqual(_type);
}
