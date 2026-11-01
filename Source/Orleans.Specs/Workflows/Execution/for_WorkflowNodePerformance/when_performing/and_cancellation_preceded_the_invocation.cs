// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Adapters;

namespace Cratis.Orleans.Workflows.Execution.for_WorkflowNodePerformance.when_performing;

public class and_cancellation_preceded_the_invocation : given.a_performance
{
    void Establish() => Run.TryStartInvocation(Execution, Attempt).Returns(false);

    Task Because() => Perform();

    [Fact] async Task should_not_perform_the_queued_effect() => await Performer!.DidNotReceive().Perform(Arg.Any<WorkflowNodeInvocation>(), Arg.Any<CancellationToken>());
    [Fact] async Task should_not_report_a_fictitious_outcome() => await Run.DidNotReceive().CompleteNode(Execution, Attempt, Arg.Any<WorkflowNodeOutcome>());
}
