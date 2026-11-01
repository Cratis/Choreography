// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Graphs;

namespace Cratis.Orleans.Workflows.Runs.Coordination.for_WorkflowCoordinator.when_an_attempt_fails;

public class and_attempts_remain : given.a_coordinator
{
    static readonly WorkflowNodePolicy _policy = new(3, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(30), 1);

    void Establish() => Admit([Trigger(), Action("a", _policy)], [Edge("trigger", "next", "a")]);

    void Because() => Fail(Root("a"));

    [Fact] void should_record_the_failed_attempt() => Recorded<WorkflowNodeAttemptFailed>().Single().Kind.ShouldEqual(WorkflowFailureKind.Error);
    [Fact] void should_record_nothing_but_the_failed_attempt() => Decision.Events.Count().ShouldEqual(1);
    [Fact] void should_make_the_next_attempt_pending() => State.Executions[Root("a")].NextAttempt.ShouldEqual(new WorkflowAttempt(2));
    [Fact] void should_not_retry_before_the_delay() => State.Executions[Root("a")].NotBefore.ShouldEqual(Now + TimeSpan.FromSeconds(30));
    [Fact] void should_not_fail_the_node() => Recorded<WorkflowNodeFailed>().ShouldBeEmpty();
    [Fact] void should_leave_the_node_waiting_for_its_retry() => StatusOf(Root("a")).ShouldEqual(NodeExecutionStatus.RetryPending);
}
