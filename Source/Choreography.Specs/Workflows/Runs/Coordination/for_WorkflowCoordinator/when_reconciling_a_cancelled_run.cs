// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Adapters;

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator;

public class when_reconciling_a_cancelled_run : given.a_coordinator
{
    void Establish()
    {
        Admit([Trigger(), Action("a"), Action("slow")], [Edge("trigger", "next", "a"), Edge("trigger", "next", "slow")]);
        Report(Root("a"), WorkflowNodeOutcome.Ambiguous("unknown"));
        Cancel();
    }

    void Because() => Reconcile(Root("a"), WorkflowNodeOutcome.Succeeded("next"));

    [Fact] void should_record_nothing() => Decision.Events.ShouldBeEmpty();
}
