// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Execution.for_WorkflowCapabilityGates;

public class when_a_waiting_call_is_cancelled : given.capability_gates
{
    readonly CancellationTokenSource _cancellation = new();
    Task<IDisposable> _waiting = null!;
    Exception? _failure;

    async Task Establish()
    {
        Options.CapabilityLimits["ability:Slack.Post"] = 1;
        await Gates.Enter(Capability("ability:Slack.Post"), CancellationToken.None);
        _waiting = Gates.Enter(Capability("ability:Slack.Post"), _cancellation.Token).AsTask();
    }

    async Task Because()
    {
        await _cancellation.CancelAsync();
        _failure = await Catch.Exception(() => _waiting);
    }

    [Fact] void should_stop_waiting() => (_failure is OperationCanceledException).ShouldBeTrue();
}
