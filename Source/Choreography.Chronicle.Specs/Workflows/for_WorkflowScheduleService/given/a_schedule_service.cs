// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Runs;
using Cratis.Choreography.Workflows.Triggers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Choreography.Chronicle.Workflows.for_WorkflowScheduleService.given;

public class a_schedule_service : Specification
{
    protected static readonly DateTimeOffset _startedAt = new(2026, 3, 2, 9, 5, 30, TimeSpan.Zero);
    protected static readonly string[] _caughtUp = [.. Enumerable.Range(0, 10).Select(minute => $"every-minute:{new DateTime(2026, 3, 2, 8, 56, 0).AddMinutes(minute):yyyyMMddHHmm}")];

    protected IWorkflows _workflows = null!;
    protected EventSourcedWorkflowsOptions _options = null!;
    protected ConcurrentQueue<string> _keys = [];
    readonly SemaphoreSlim _started = new(0);

    void Establish()
    {
        _workflows = Substitute.For<IWorkflows>();
        _workflows.Start(Arg.Any<WorkflowScope>(), Arg.Any<WorkflowId>(), Arg.Any<WorkflowTrigger>()).Returns(callInfo =>
        {
            _keys.Enqueue(callInfo.Arg<WorkflowTrigger>().Deduplication.Value);
            _started.Release();
            return Task.FromResult(WorkflowAdmission.To(new WorkflowRunId("run")));
        });
        _options = new EventSourcedWorkflowsOptions();
        _options.Schedule(new WorkflowSchedule("every-minute", new WorkflowScope("scope-1"), new WorkflowId("workflow-1"), CronExpression.Parse("* * * * *")));
    }

    protected WorkflowScheduleService ServiceOn(TimeProvider clock) => new(Options.Create(_options), _workflows, clock, NullLogger<WorkflowScheduleService>.Instance);

    protected async Task WaitForStarts(int count)
    {
        for (var start = 0; start < count; start++)
        {
            if (!await _started.WaitAsync(TimeSpan.FromSeconds(10)))
            {
                throw new TimeoutException($"Only {start} of {count} runs were started");
            }
        }
    }
}
