// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Triggers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Choreography.Chronicle.Workflows;

/// <summary>
/// Represents the background service that starts workflows on their schedules.
/// </summary>
/// <param name="options">The <see cref="EventSourcedWorkflowsOptions"/>.</param>
/// <param name="workflows">The <see cref="IWorkflows"/> runs are admitted through.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/>.</param>
/// <param name="logger">The logger.</param>
/// <remarks>
/// Every silo runs it. The deduplication key of a scheduled run names the schedule and the minute it fell due, and the same key
/// is the same run, so the silos starting the same minute admit one run between them. Minutes that fell due while no silo
/// was running are started when the first one is, up to <see cref="MaximumCatchUp"/> back.
/// </remarks>
public class WorkflowScheduleService(
    IOptions<EventSourcedWorkflowsOptions> options,
    IWorkflows workflows,
    TimeProvider timeProvider,
    ILogger<WorkflowScheduleService> logger) : BackgroundService
{
    /// <summary>
    /// The longest a minute that fell due is still started afterwards.
    /// </summary>
    public static readonly TimeSpan MaximumCatchUp = TimeSpan.FromMinutes(10);

    /// <summary>
    /// How often the schedules are looked at.
    /// </summary>
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Starts the runs of every schedule that fell due after one time and up to another.
    /// </summary>
    /// <param name="after">The time to start after, exclusive.</param>
    /// <param name="until">The time to stop at, inclusive.</param>
    /// <returns>Awaitable task.</returns>
    public async Task StartDue(DateTimeOffset after, DateTimeOffset until)
    {
        if (until - after > MaximumCatchUp)
        {
            after = until - MaximumCatchUp;
        }

        foreach (var schedule in options.Value.Schedules)
        {
            foreach (var due in schedule.Cron.OccurrencesBetween(after, until))
            {
                await workflows.Start(schedule.Scope, schedule.Workflow, TriggerFor(schedule, due));
            }
        }
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (options.Value.Schedules.Count == 0)
        {
            return;
        }

        var handled = timeProvider.GetUtcNow();
        try
        {
            using var timer = new PeriodicTimer(Interval, timeProvider);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var now = timeProvider.GetUtcNow();
                try
                {
                    await StartDue(handled, now);
                    handled = now;
                }
                catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
                {
                    logger.ScheduleFailed(exception);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // The silo is stopping.
        }
    }

    static WorkflowTrigger TriggerFor(WorkflowSchedule schedule, DateTimeOffset due) => new(
        WorkflowTriggerKind.Schedule,
        new TriggerSourceKey(schedule.Name),
        new WorkflowDeduplicationKey($"{schedule.Name}:{due.UtcDateTime:yyyyMMddHHmm}"),
        new WorkflowSubjectReference(new WorkflowSubjectKind("schedule"), new WorkflowSubjectId(schedule.Name)),
        WorkflowContractReference.Any,
        WorkflowData.Empty,
        [],
        new WorkflowCorrelationKey($"{schedule.Name}:{due.UtcDateTime:yyyyMMddHHmm}"),
        WorkflowCausationKey.None,
        WorkflowActor.System,
        due);
}
