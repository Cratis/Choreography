// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Runs.for_WorkflowRunGrain;

public class when_admitting_the_same_trigger_twice : given.a_run
{
    async Task Because()
    {
        await Grain.Admit(Definition, Trigger);
        await Grain.Admit(Definition, Trigger);
    }

    [Fact] void should_record_one_admission() => History.Select(item => item.Fact).OfType<WorkflowRunAdmitted>().Count().ShouldEqual(1);
    [Fact] void should_append_once() => Appends.ShouldEqual(1);
}
