// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Choreography.Storage.Sql.for_JobStepStorage.given;
using Cratis.Choreography.Storage.Sql.Jobs;

namespace Cratis.Choreography.Storage.Sql.for_JobStepStorage.when_reading_a_step;

/// <summary>
/// Specifies that rows written with plain serializer options, before the serializer contract, can still be read.
/// </summary>
public class that_was_written_by_the_previous_version : job_steps_in_storage
{
    JobStepId _baseStepId;
    JobStepId _derivedStepId;
    JobStepState _base;
    a_derived_job_step_state _derived;

    async Task Establish()
    {
        _baseStepId = JobStepId.New();
        _derivedStepId = JobStepId.New();

        var baseState = AJobStepState(_jobId, _baseStepId, JobStepStatus.Running);
        var derivedState = new a_derived_job_step_state
        {
            Id = new JobStepIdentifier(_jobId, _derivedStepId),
            Type = new JobStepType("some-step-type"),
            Name = (JobStepName)"some step",
            Target = "some target",
            Progress = 42,
            IsPrepared = true
        };

        await using var context = CreateContext();
        context.JobSteps.Add(Row(_baseStepId, JsonSerializer.Serialize(baseState)));
        context.JobSteps.Add(Row(_derivedStepId, JsonSerializer.Serialize(derivedState, derivedState.GetType())));
        await context.SaveChangesAsync();
    }

    async Task Because()
    {
        _base = (await _storage.Read<JobStepState>(_jobId, _baseStepId)).AsT0;
        _derived = (await _storage.Read<a_derived_job_step_state>(_jobId, _derivedStepId)).AsT0;
    }

    [Fact] void should_read_the_base_state() => _base.IsPrepared.ShouldBeTrue();
    [Fact] void should_read_the_derived_text() => _derived.Target.ShouldEqual("some target");
    [Fact] void should_read_the_derived_number() => _derived.Progress.ShouldEqual(42);

    JobStep Row(JobStepId jobStepId, string json) =>
        new()
        {
            Id = Guid.NewGuid(),
            JobId = _jobId.Value,
            JobStepId = jobStepId.Value,
            Type = "some-step-type",
            Name = "some step",
            Status = JobStepStatus.Running,
            IsPrepared = true,
            StateJson = json
        };
}
