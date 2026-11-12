// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Sql.for_JobStepStorage.given;

namespace Cratis.Choreography.Storage.Sql.for_JobStepStorage.when_reading_a_step;

/// <summary>
/// Specifies that a step moved to failed keeps a concept-keyed dictionary.
/// </summary>
public class that_was_moved_to_failed_holding_a_concept_keyed_dictionary : job_steps_in_storage
{
    JobStepId _jobStepId;
    bool _moved;
    a_job_step_state_with_concepts_and_dictionaries _result;

    async Task Establish()
    {
        _jobStepId = JobStepId.New();
        var state = a_job_step_state_with_concepts_and_dictionaries.Populated(new JobStepIdentifier(_jobId, _jobStepId));
        await _storage.Save(_jobId, _jobStepId, state);
    }

    async Task Because()
    {
        var state = a_job_step_state_with_concepts_and_dictionaries.Populated(new JobStepIdentifier(_jobId, _jobStepId));
        state.Counts[new a_step_concept("c")] = 7;
        _moved = (await _storage.MoveToFailed(_jobId, _jobStepId, state)).IsT0;
        _result = (await _storage.Read<a_job_step_state_with_concepts_and_dictionaries>(_jobId, _jobStepId)).AsT0;
    }

    [Fact] void should_move_it() => _moved.ShouldBeTrue();
    [Fact] void should_keep_the_counts() => _result.Counts[new a_step_concept("c")].ShouldEqual(7L);
}
