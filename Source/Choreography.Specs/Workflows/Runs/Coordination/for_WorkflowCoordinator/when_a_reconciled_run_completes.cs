// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Adapters;

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator;

public class when_a_reconciled_run_completes : given.a_coordinator
{
    void Establish()
    {
        Admit([Trigger(), Action("a"), Action("b")], [Edge("trigger", "next", "a"), Edge("a", "next", "b")]);
        Report(Root("a"), WorkflowNodeOutcome.Ambiguous("unknown"));
        Reconcile(Root("a"), WorkflowNodeOutcome.Succeeded("next"));
    }

    void Because() => Succeed(Root("b"));

    [Fact] void should_finish_the_run_as_succeeded() => Recorded<WorkflowRunSucceeded>().ShouldNotBeEmpty();
}
