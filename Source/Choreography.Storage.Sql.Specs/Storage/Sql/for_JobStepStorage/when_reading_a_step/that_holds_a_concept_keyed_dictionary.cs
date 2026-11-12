// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Choreography.Storage.Sql.for_JobStepStorage.given;

namespace Cratis.Choreography.Storage.Sql.for_JobStepStorage.when_reading_a_step;

/// <summary>
/// Specifies that a step state holding concepts and concept-keyed dictionaries is saved and read back intact.
/// </summary>
public class that_holds_a_concept_keyed_dictionary : job_steps_in_storage
{
    JobStepId _jobStepId;
    bool _saved;
    a_job_step_state_with_concepts_and_dictionaries _result;

    void Establish() => _jobStepId = JobStepId.New();

    async Task Because()
    {
        JobStepState state = a_job_step_state_with_concepts_and_dictionaries.Populated(new JobStepIdentifier(_jobId, _jobStepId));
        _saved = (await _storage.Save(_jobId, _jobStepId, state)).IsT0;
        _result = (await _storage.Read<a_job_step_state_with_concepts_and_dictionaries>(_jobId, _jobStepId)).AsT0;
    }

    [Fact] void should_save_it() => _saved.ShouldBeTrue();
    [Fact] void should_keep_the_concept() => _result.Concept.ShouldEqual(new a_step_concept("first"));
    [Fact] void should_keep_the_concept_list() => string.Join(',', _result.Concepts.Select(_ => _.Value)).ShouldEqual("one,two");
    [Fact] void should_keep_the_counts() => string.Join(',', _result.Counts.OrderBy(_ => _.Key.Value).Select(_ => $"{_.Key.Value}={_.Value}")).ShouldEqual("a=3,b=5");
    [Fact] void should_keep_the_records() => string.Join(',', _result.Records[new a_step_concept("a")].Select(_ => $"{_.Name.Value}={_.Count}")).ShouldEqual("x=1,y=2");
}
