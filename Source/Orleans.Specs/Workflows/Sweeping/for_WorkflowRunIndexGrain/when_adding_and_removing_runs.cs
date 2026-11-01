// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.TestKit;

namespace Cratis.Orleans.Workflows.Sweeping.for_WorkflowRunIndexGrain;

public class when_adding_and_removing_runs : Specification
{
    IReadOnlyList<WorkflowRunId> _active = null!;

    async Task Because()
    {
        var silo = new TestKitSilo();
        var grain = await silo.CreateGrainAsync<WorkflowRunIndexGrain>("scope-1");
        await grain.Add(new WorkflowRunId("one"));
        await grain.Add(new WorkflowRunId("two"));
        await grain.Add(new WorkflowRunId("two"));
        await grain.Remove(new WorkflowRunId("one"));
        await grain.Remove(new WorkflowRunId("never-added"));
        _active = await grain.GetActive();
    }

    [Fact] void should_hold_the_runs_that_are_not_over() => _active.Select(run => run.Value).ShouldEqual(["two"]);
}
