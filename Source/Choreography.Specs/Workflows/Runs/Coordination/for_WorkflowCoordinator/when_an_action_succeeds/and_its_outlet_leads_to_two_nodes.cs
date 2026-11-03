// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator.when_an_action_succeeds;

public class and_its_outlet_leads_to_two_nodes : given.a_coordinator
{
    void Establish() => Admit(
        [Trigger(), Action("a"), Action("b"), Action("c")],
        [Edge("trigger", "next", "a"), Edge("a", "next", "b"), Edge("a", "next", "c")]);

    void Because() => Succeed(Root("a"), output: """{"plan":"ship it"}""");

    [Fact] void should_schedule_both() => ScheduledNow.ShouldContainOnly(Root("b"), Root("c"));
    [Fact] void should_schedule_them_into_one_wave() => Recorded<WorkflowNodeScheduled>().Select(scheduled => scheduled.Wave.Value).Distinct().ShouldContainOnly(2);
    [Fact] void should_hand_two_items_to_the_wave() => Decision.ToSchedule.Count().ShouldEqual(2);
    [Fact] void should_give_each_the_output_it_follows() => Decision.ToSchedule.All(item => item.Input.Contains("ship it", StringComparison.Ordinal)).ShouldBeTrue();
}
