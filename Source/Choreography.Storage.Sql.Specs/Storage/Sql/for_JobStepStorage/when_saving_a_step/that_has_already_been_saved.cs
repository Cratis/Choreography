// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Sql.for_JobStepStorage.given;

namespace Cratis.Choreography.Storage.Sql.for_JobStepStorage.when_saving_a_step;

/// <summary>
/// Specifies that saving a step again replaces its state instead of adding a row.
/// </summary>
public class that_has_already_been_saved : job_steps_in_storage
{
    JobStepId _jobStepId;
    a_job_step_state_with_concepts_and_dictionaries _result;
    int _rows;

    async Task Establish()
    {
        _jobStepId = JobStepId.New();
        await _storage.Save(_jobId, _jobStepId, a_job_step_state_with_concepts_and_dictionaries.Populated(new JobStepIdentifier(_jobId, _jobStepId)));
    }

    async Task Because()
    {
        var state = a_job_step_state_with_concepts_and_dictionaries.Populated(new JobStepIdentifier(_jobId, _jobStepId));
        state.Concept = new("changed");
        state.Counts[new a_step_concept("c")] = 9;
        await _storage.Save(_jobId, _jobStepId, state);
        _result = (await _storage.Read<a_job_step_state_with_concepts_and_dictionaries>(_jobId, _jobStepId)).AsT0;
        await using var context = CreateContext();
        _rows = context.JobSteps.Count(_ => _.JobStepId == _jobStepId.Value);
    }

    [Fact] void should_keep_one_row() => _rows.ShouldEqual(1);
    [Fact] void should_hold_the_latest_concept() => _result.Concept.ShouldEqual(new a_step_concept("changed"));
    [Fact] void should_hold_the_added_entry() => _result.Counts[new a_step_concept("c")].ShouldEqual(9L);
}
