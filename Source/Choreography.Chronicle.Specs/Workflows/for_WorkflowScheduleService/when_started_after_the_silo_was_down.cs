// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Chronicle.Workflows.for_WorkflowScheduleService.given;

namespace Cratis.Choreography.Chronicle.Workflows.for_WorkflowScheduleService;

public class when_started_after_the_silo_was_down : a_schedule_service
{
    WorkflowScheduleService _service = null!;

    async Task Because()
    {
        var clock = new ManualClock(_startedAt);
        _service = ServiceOn(clock);
        await _service.StartAsync(CancellationToken.None);
        await clock.Tick(WorkflowScheduleService.Interval);
        await WaitForStarts(_caughtUp.Length);
        await _service.StopAsync(CancellationToken.None);
    }

    [Fact] void should_start_the_minutes_that_fell_due_within_the_catch_up_window_on_the_first_tick() => _keys.ShouldEqual(_caughtUp);
    [Fact] void should_stop_running() => _service.ExecuteTask!.IsCompletedSuccessfully.ShouldBeTrue();
}
