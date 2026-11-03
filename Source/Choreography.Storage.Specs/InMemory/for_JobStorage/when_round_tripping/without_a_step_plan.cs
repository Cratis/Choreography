// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage.Jobs;

namespace Cratis.Choreography.Storage.InMemory.for_JobStorage.when_round_tripping;

public class without_a_step_plan : given.jobs_in_storage
{
    JobState _result;

    async Task Establish() => (await _storage.Save(_id, new JobState { Id = _id, Type = _type })).IsSuccess.ShouldBeTrue();

    async Task Because() => _result = (await _storage.Read<JobState>(_id)).AsT0;

    [Fact] void should_preserve_a_missing_plan() => _result.StepPlan.ShouldBeNull();
}
