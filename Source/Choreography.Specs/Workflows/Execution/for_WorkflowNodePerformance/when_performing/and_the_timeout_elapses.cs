// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Runs;

namespace Cratis.Choreography.Workflows.Execution.for_WorkflowNodePerformance.when_performing;

/// <summary>
/// A performer that ignores its cancellation token and never answers - the attempt still ends at its timeout.
/// </summary>
public class and_the_timeout_elapses : given.a_performance
{
    readonly TaskCompletionSource<WorkflowNodeOutcome> _never = new();

    void Establish() => Performer!.Perform(Arg.Any<WorkflowNodeInvocation>(), Arg.Any<CancellationToken>()).Returns(_never.Task);

    Task Because() => Perform();

    [Fact] Task should_report_a_timeout() => Run.Received(1).FailAttempt(Execution, Attempt, WorkflowFailureKind.TimedOut, Arg.Any<WorkflowFailureReason>());
    [Fact] Task should_not_report_an_outcome() => Run.DidNotReceive().CompleteNode(Arg.Any<NodeExecutionKey>(), Arg.Any<WorkflowAttempt>(), Arg.Any<WorkflowNodeOutcome>());
}
