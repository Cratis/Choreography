// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Chronicle.Workflows.for_WorkflowScheduleService.given;
using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Triggers;

namespace Cratis.Choreography.Chronicle.Workflows.for_WorkflowScheduleService;

public class when_started_without_schedules : a_schedule_service
{
    WorkflowScheduleService _service = null!;

    void Establish() => _options = new EventSourcedWorkflowsOptions();

    async Task Because()
    {
        _service = ServiceOn(new ManualClock(_startedAt));
        await _service.StartAsync(CancellationToken.None);
        await _service.ExecuteTask!;
    }

    [Fact] void should_stop_running() => _service.ExecuteTask!.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_not_start_any_run() => _workflows.DidNotReceive().Start(Arg.Any<WorkflowScope>(), Arg.Any<WorkflowId>(), Arg.Any<WorkflowTrigger>());
}
