// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.Choreography.Workflows.Runs;

namespace Cratis.Choreography.Workflows.Execution.for_WorkflowNodePerformance.when_performing;

/// <summary>
/// The capability's gate never has room - the attempt times out without the performer being called.
/// </summary>
public class and_the_capability_gate_is_full : given.a_performance
{
    Task Because() => PerformThrough(new FullGate());

    [Fact] Task should_report_a_timeout() => Run.Received(1).FailAttempt(Execution, Attempt, WorkflowFailureKind.TimedOut, Arg.Any<WorkflowFailureReason>());
    [Fact] Task should_not_call_the_performer() => Performer!.DidNotReceive().Perform(Arg.Any<WorkflowNodeInvocation>(), Arg.Any<CancellationToken>());

    sealed class FullGate : IWorkflowCapabilityGates
    {
        public async ValueTask<IDisposable> Enter(CapabilityReference capability, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return null!;
        }
    }
}
