// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Adapters;

namespace Cratis.Orleans.Workflows.Runs.Coordination.for_WorkflowCoordinator;

public class when_an_outcome_arrives_twice : given.a_coordinator
{
    void Establish()
    {
        Admit([Trigger(), Action("a"), Action("b")], [Edge("trigger", "next", "a"), Edge("a", "next", "b")]);
        Succeed(Root("a"));
    }

    void Because() => Report(Root("a"), WorkflowAttempt.First, WorkflowNodeOutcome.Succeeded("next"));

    [Fact] void should_record_nothing() => Decision.Events.ShouldBeEmpty();
    [Fact] void should_schedule_nothing() => Decision.ToSchedule.ShouldBeEmpty();
}
