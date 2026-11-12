// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Sql.for_JobStepStorage.given;

namespace Cratis.Choreography.Storage.Sql.for_JobStepStorage.when_saving_a_step;

/// <summary>
/// Specifies that a step state round-trips with host options that omit nulls and carry no step converters of their own.
/// </summary>
public class through_host_options : job_steps_in_storage
{
    JobStepId _jobStepId;
    JsonSerializerOptions _hostOptions;
    a_job_step_state_with_concepts_and_dictionaries _result;
    a_derived_job_step_state _viaOldConstructor;

    void Establish()
    {
        _jobStepId = JobStepId.New();
        _hostOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    async Task Because()
    {
        var hosted = new Sql.Jobs.JobStepStorage(CreateContext, _hostOptions);
        await hosted.Save(_jobId, _jobStepId, a_job_step_state_with_concepts_and_dictionaries.Populated(new JobStepIdentifier(_jobId, _jobStepId)));
        _result = (await hosted.Read<a_job_step_state_with_concepts_and_dictionaries>(_jobId, _jobStepId)).AsT0;

        var oldStepId = JobStepId.New();
        await _storage.Save(_jobId, oldStepId, new a_derived_job_step_state
        {
            Id = new JobStepIdentifier(_jobId, oldStepId),
            Type = new JobStepType("some-step-type"),
            Name = (JobStepName)"some step",
            Target = "some target"
        });
        _viaOldConstructor = (await _storage.Read<a_derived_job_step_state>(_jobId, oldStepId)).AsT0;
    }

    [Fact] void should_keep_the_concept() => _result.Concept.ShouldEqual(new a_step_concept("first"));
    [Fact] void should_keep_the_counts() => _result.Counts[new a_step_concept("b")].ShouldEqual(5L);
    [Fact] void should_work_through_the_constructor_without_options() => _viaOldConstructor.Target.ShouldEqual("some target");
}
