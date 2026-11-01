// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Runs.Coordination.for_WorkflowCoordinator.when_completing_an_operation;

public class with_invalid_direct_output : given.a_coordinator
{
    void Establish() => Admit([Trigger(), Action("action")], [Edge("trigger", "next", "action")]);

    void Because() => Apply(WorkflowCoordinator.CompleteOperation(Run, State, Root("action"), AttemptOf(Root("action")), "register", "not JSON", Now));

    [Fact] void should_not_record_an_operation_success() => Recorded<WorkflowNodeOperationCompleted>().ShouldBeEmpty();
    [Fact] void should_record_invalid_output() => Recorded<WorkflowNodeFailed>().Single().Kind.ShouldEqual(WorkflowFailureKind.InvalidOutput);
    [Fact] void should_finish_the_failed_run() => State.Status.ShouldEqual(WorkflowRunStatus.Failed);
}
