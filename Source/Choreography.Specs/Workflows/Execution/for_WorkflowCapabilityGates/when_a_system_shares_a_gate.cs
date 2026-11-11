// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Execution.for_WorkflowCapabilityGates;

public class when_a_system_shares_a_gate : given.capability_gates
{
    Task<IDisposable> _anotherAbility = null!;
    Task<IDisposable> _anotherSystem = null!;

    async Task Establish()
    {
        Options.CapabilityLimits["ability:Slack.*"] = 1;
        await Gates.Enter(Capability("ability:Slack.Post"), CancellationToken.None);
    }

    void Because()
    {
        _anotherAbility = Gates.Enter(Capability("ability:Slack.Read"), CancellationToken.None).AsTask();
        _anotherSystem = Gates.Enter(Capability("ability:Teams.Post"), CancellationToken.None).AsTask();
    }

    [Fact] void should_hold_back_another_ability_of_the_same_system() => _anotherAbility.IsCompleted.ShouldBeFalse();
    [Fact] void should_not_hold_back_another_system() => _anotherSystem.IsCompletedSuccessfully.ShouldBeTrue();
}
