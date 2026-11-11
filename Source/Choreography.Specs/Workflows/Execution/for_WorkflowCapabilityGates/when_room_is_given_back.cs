// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Execution.for_WorkflowCapabilityGates;

public class when_room_is_given_back : given.capability_gates
{
    IDisposable _first = null!;
    Task<IDisposable> _second = null!;

    async Task Establish()
    {
        Options.CapabilityLimits["ability:Slack.Post"] = 1;
        _first = await Gates.Enter(Capability("ability:Slack.Post"), CancellationToken.None);
        _second = Gates.Enter(Capability("ability:Slack.Post"), CancellationToken.None).AsTask();
    }

    async Task Because()
    {
        _first.Dispose();
        _first.Dispose();
        (await _second.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)).Dispose();
    }

    [Fact] void should_let_the_waiting_call_in() => _second.IsCompletedSuccessfully.ShouldBeTrue();
}
