// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Execution.for_WorkflowCapabilityGates;

public class when_an_exact_rule_and_a_prefix_rule_cover_a_capability : given.capability_gates
{
    Task<IDisposable> _second = null!;

    async Task Establish()
    {
        Options.CapabilityLimits["ability:Slack.*"] = 1;
        Options.CapabilityLimits["ability:Slack.Post"] = 2;
        await Gates.Enter(Capability("ability:Slack.Post"), CancellationToken.None);
    }

    void Because() => _second = Gates.Enter(Capability("ability:Slack.Post"), CancellationToken.None).AsTask();

    [Fact] void should_use_the_exact_rule() => _second.IsCompletedSuccessfully.ShouldBeTrue();
}
