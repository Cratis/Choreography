// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator.when_admitting;

public class and_the_trigger_leads_to_an_action : given.a_coordinator
{
    void Because() => Admit([Trigger(), Action("plan")], [Edge("trigger", "next", "plan")]);

    [Fact] void should_record_the_admission_first() => Decision.Events.First().ShouldBeOfExactType<WorkflowRunAdmitted>();
    [Fact] void should_complete_the_trigger_through_next() => Recorded<WorkflowNodeSucceeded>().Single().ShouldEqual(Recorded<WorkflowNodeSucceeded>().Single() with { Execution = Root("trigger"), Outlet = "next" });
    [Fact] void should_schedule_the_action() => ScheduledNow.ShouldContainOnly(Root("plan"));
    [Fact] void should_schedule_it_into_the_first_wave() => Decision.Wave.ShouldEqual(1);
    [Fact] void should_schedule_it_into_the_first_wave_job() => Recorded<WorkflowNodeScheduled>().Single().Job.ShouldEqual(CoordinatorDecision.JobFor(Run.Run, 1));
    [Fact] void should_hand_the_item_to_the_wave() => Decision.ToSchedule.Single().Execution.ShouldEqual(Root("plan").ToString());
    [Fact] void should_give_the_item_the_tenant_qualified_run() => Decision.ToSchedule.Single().Run.ShouldEqual(Run.ToString());
}
