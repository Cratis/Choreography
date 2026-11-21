// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Chronicle.Workflows.for_WorkflowScheduleService.given;

namespace Cratis.Choreography.Chronicle.Workflows.for_WorkflowScheduleService;

public class when_started_again_within_the_catch_up_window : a_schedule_service
{
    string[] _first = [];
    string[] _second = [];

    async Task Because()
    {
        _first = await StartAndCatchUp(_startedAt);
        _second = await StartAndCatchUp(_startedAt.AddSeconds(5));
    }

    [Fact] void should_start_the_same_minutes_so_they_are_deduplicated_into_the_same_runs() => _second.ShouldEqual(_first);

    async Task<string[]> StartAndCatchUp(DateTimeOffset at)
    {
        var clock = new ManualClock(at);
        var service = ServiceOn(clock);
        await service.StartAsync(CancellationToken.None);
        await clock.Tick(WorkflowScheduleService.Interval);
        await WaitForStarts(_caughtUp.Length);
        await service.StopAsync(CancellationToken.None);
        string[] keys = [.. _keys];
        _keys.Clear();
        return keys;
    }
}
