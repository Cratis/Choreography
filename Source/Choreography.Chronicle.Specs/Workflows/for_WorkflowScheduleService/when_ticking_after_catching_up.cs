// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Chronicle.Workflows.for_WorkflowScheduleService.given;

namespace Cratis.Choreography.Chronicle.Workflows.for_WorkflowScheduleService;

public class when_ticking_after_catching_up : a_schedule_service
{
    async Task Because()
    {
        var clock = new ManualClock(_startedAt);
        var service = ServiceOn(clock);
        await service.StartAsync(CancellationToken.None);
        await clock.Tick(WorkflowScheduleService.Interval);
        await WaitForStarts(_caughtUp.Length);
        await clock.Tick(WorkflowScheduleService.Interval);
        await WaitForStarts(1);
        await service.StopAsync(CancellationToken.None);
    }

    [Fact] void should_start_the_next_minute_once() => _keys.Skip(_caughtUp.Length).ShouldEqual(["every-minute:202603020906"]);
    [Fact] void should_not_start_a_minute_twice() => _keys.Distinct().Count().ShouldEqual(_keys.Count);
}
