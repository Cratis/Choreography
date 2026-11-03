// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Runs;
using Cratis.Choreography.Workflows.Triggers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Choreography.Chronicle.Workflows.for_WorkflowScheduleService;

public class when_the_silo_was_down_for_a_long_time : Specification
{
    List<WorkflowTrigger> _triggers = [];

    async Task Because()
    {
        var workflows = Substitute.For<IWorkflows>();
        workflows.Start(Arg.Any<WorkflowScope>(), Arg.Any<WorkflowId>(), Arg.Do<WorkflowTrigger>(_triggers.Add)).Returns(Task.FromResult(WorkflowAdmission.To(new WorkflowRunId("run"))));
        var options = new EventSourcedWorkflowsOptions();
        options.Schedule(new WorkflowSchedule("every-minute", new WorkflowScope("scope-1"), new WorkflowId("workflow-1"), CronExpression.Parse("* * * * *")));
        var service = new WorkflowScheduleService(Options.Create(options), workflows, TimeProvider.System, NullLogger<WorkflowScheduleService>.Instance);
        await service.StartDue(new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 3, 2, 9, 0, 0, TimeSpan.Zero));
    }

    [Fact] void should_only_catch_up_the_most_recent_minutes() => _triggers.Count.ShouldEqual((int)WorkflowScheduleService.MaximumCatchUp.TotalMinutes);
}
