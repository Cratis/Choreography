// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Adapters;

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator;

public class when_reconciling_an_execution_that_is_not_ambiguous : given.a_coordinator
{
    void Establish()
    {
        Admit([Trigger(), Action("a")], [Edge("trigger", "next", "a")]);
        Succeed(Root("a"));
    }

    void Because() => Reconcile(Root("a"), WorkflowNodeOutcome.Failed("changed my mind"));

    [Fact] void should_record_nothing() => Decision.Events.ShouldBeEmpty();
}
