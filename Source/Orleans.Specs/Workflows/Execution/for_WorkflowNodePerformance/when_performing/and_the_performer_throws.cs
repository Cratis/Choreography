// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Adapters;
using Cratis.Orleans.Workflows.Runs;

namespace Cratis.Orleans.Workflows.Execution.for_WorkflowNodePerformance.when_performing;

public class and_the_performer_throws : given.a_performance
{
    void Establish() => Performer!
        .Perform(Arg.Any<WorkflowNodeInvocation>(), Arg.Any<CancellationToken>())
        .Returns<Task<WorkflowNodeOutcome>>(_ => throw new InvalidOperationException("the provider is down"));

    Task Because() => Perform();

    [Fact] Task should_report_a_failure() => Run.Received(1).CompleteNode(Execution, Attempt, Arg.Is<WorkflowNodeOutcome>(outcome => outcome.Status == WorkflowNodeOutcomeStatus.Failed && outcome.Reason == new WorkflowFailureReason("the provider is down")));
    [Fact] Task should_not_report_a_timeout() => Run.DidNotReceive().FailAttempt(Arg.Any<NodeExecutionKey>(), Arg.Any<WorkflowAttempt>(), Arg.Any<WorkflowFailureKind>(), Arg.Any<WorkflowFailureReason>());
}
