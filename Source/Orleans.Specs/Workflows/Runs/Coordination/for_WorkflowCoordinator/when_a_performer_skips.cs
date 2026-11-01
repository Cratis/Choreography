// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Adapters;

namespace Cratis.Orleans.Workflows.Runs.Coordination.for_WorkflowCoordinator;

public class when_a_performer_skips : given.a_coordinator
{
    void Establish() => Admit([Trigger(), Action("a"), Action("b")], [Edge("trigger", "next", "a"), Edge("a", "next", "b")]);

    void Because() => Report(Root("a"), WorkflowNodeOutcome.Skipped("nothing to do"));

    [Fact] void should_record_the_skip() => Recorded<WorkflowNodeSkipped>().Single().Execution.ShouldEqual(Root("a"));
    [Fact] void should_not_record_a_failure() => Recorded<WorkflowNodeFailed>().ShouldBeEmpty();
    [Fact] void should_not_continue_past_it() => ScheduledNow.ShouldBeEmpty();
    [Fact] void should_succeed_the_run() => Recorded<WorkflowRunSucceeded>().ShouldNotBeEmpty();
}
