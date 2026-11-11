// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Adapters;

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator;

public class when_reconciling_with_another_ambiguous_outcome : given.a_coordinator
{
    void Establish()
    {
        Admit([Trigger(), Action("a")], [Edge("trigger", "next", "a")]);
        Report(Root("a"), WorkflowNodeOutcome.Ambiguous("unknown"));
    }

    void Because() => Reconcile(Root("a"), WorkflowNodeOutcome.Ambiguous("still unknown"));

    [Fact] void should_record_nothing() => Decision.Events.ShouldBeEmpty();
}
