// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Execution.for_WorkflowCapabilityGates;

public class when_entering_a_capability_without_a_limit : given.capability_gates
{
    readonly List<Task<IDisposable>> _entered = [];

    void Establish() => Options.CapabilityLimits["ability:Slack.Post"] = 1;

    void Because()
    {
        for (var index = 0; index < 5; index++)
        {
            _entered.Add(Gates.Enter(Capability("ability:Teams.Post"), CancellationToken.None).AsTask());
        }
    }

    [Fact] void should_let_every_call_in() => _entered.TrueForAll(task => task.IsCompletedSuccessfully).ShouldBeTrue();
}
