// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Execution.for_WorkflowCapabilityGates;

public class when_a_capability_is_at_its_limit : given.capability_gates
{
    Task<IDisposable> _second = null!;
    Task<IDisposable> _otherCapability = null!;

    async Task Establish()
    {
        Options.CapabilityLimits["ability:Slack.Post"] = 1;
        await Gates.Enter(Capability("ability:Slack.Post"), CancellationToken.None);
    }

    void Because()
    {
        _second = Gates.Enter(Capability("ability:Slack.Post"), CancellationToken.None).AsTask();
        _otherCapability = Gates.Enter(Capability("ability:Teams.Post"), CancellationToken.None).AsTask();
    }

    [Fact] void should_hold_the_call_back() => _second.IsCompleted.ShouldBeFalse();
    [Fact] void should_not_hold_back_other_capabilities() => _otherCapability.IsCompletedSuccessfully.ShouldBeTrue();
}
