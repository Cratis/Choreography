// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Choreography.Storage.Sql.for_JobStepStorage.given;

namespace Cratis.Choreography.Storage.Sql.for_JobStepStorage.when_reading_a_step;

/// <summary>
/// Specifies that a derived step state keeps its own properties when it is moved to failed through the base state type.
/// </summary>
public class that_was_moved_to_failed_through_the_base_state_type : job_steps_in_storage
{
    JobStepId _jobStepId;
    a_derived_job_step_state _result;

    async Task Establish()
    {
        _jobStepId = JobStepId.New();
        JobStepState state = new a_derived_job_step_state
        {
            Id = new JobStepIdentifier(_jobId, _jobStepId),
            Type = new JobStepType("some-step-type"),
            Name = (JobStepName)"some step",
            Status = JobStepStatus.Failed,
            IsPrepared = true,
            Target = "some target"
        };
        await _storage.Save(_jobId, _jobStepId, AJobStepState(_jobId, _jobStepId, JobStepStatus.Running));
        await _storage.MoveToFailed(_jobId, _jobStepId, state);
    }

    async Task Because() => _result = (await _storage.Read<a_derived_job_step_state>(_jobId, _jobStepId)).AsT0;

    [Fact] void should_keep_the_derived_properties() => _result.Target.ShouldEqual("some target");
}
