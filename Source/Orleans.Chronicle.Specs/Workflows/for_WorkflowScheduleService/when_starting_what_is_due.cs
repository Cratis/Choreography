// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows;
using Cratis.Orleans.Workflows.Runs;
using Cratis.Orleans.Workflows.Triggers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Orleans.Chronicle.Workflows.for_WorkflowScheduleService;

public class when_starting_what_is_due : Specification
{
    IWorkflows _workflows = null!;
    List<WorkflowTrigger> _triggers = [];

    async Task Because()
    {
        _workflows = Substitute.For<IWorkflows>();
        _workflows.Start(Arg.Any<WorkflowScope>(), Arg.Any<WorkflowId>(), Arg.Do<WorkflowTrigger>(_triggers.Add)).Returns(Task.FromResult(WorkflowAdmission.To(new WorkflowRunId("run"))));
        var options = new EventSourcedWorkflowsOptions();
        options.Schedule(new WorkflowSchedule("nightly", new WorkflowScope("scope-1"), new WorkflowId("workflow-1"), CronExpression.Parse("*/5 * * * *")));
        var service = new WorkflowScheduleService(Options.Create(options), _workflows, TimeProvider.System, NullLogger<WorkflowScheduleService>.Instance);
        await service.StartDue(new DateTimeOffset(2026, 3, 2, 9, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 3, 2, 9, 10, 20, TimeSpan.Zero));
    }

    [Fact] void should_start_a_run_for_each_minute_that_fell_due() => _triggers.Count.ShouldEqual(2);
    [Fact] void should_name_the_schedule_and_the_minute_in_the_deduplication_key() => _triggers.Select(trigger => trigger.Deduplication.Value).ShouldEqual(["nightly:202603020905", "nightly:202603020910"]);
    [Fact] void should_start_them_as_scheduled() => _triggers.TrueForAll(trigger => trigger.Kind == WorkflowTriggerKind.Schedule).ShouldBeTrue();
    [Fact] void should_start_them_in_the_scope_of_the_schedule() => _workflows.Received(2).Start(new WorkflowScope("scope-1"), new WorkflowId("workflow-1"), Arg.Any<WorkflowTrigger>());
}
