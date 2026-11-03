// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Runs;
using Microsoft.Extensions.Logging;

namespace Cratis.Choreography.Workflows.Sweeping;

/// <summary>
/// Represents an implementation of <see cref="IWorkflowRunSweeper"/> that asks the runs the scope's <see cref="IWorkflowRunIndex"/> holds.
/// </summary>
/// <param name="grains">The <see cref="IGrainFactory"/>.</param>
/// <param name="logger">The logger.</param>
public class WorkflowRunSweeper(IGrainFactory grains, ILogger<WorkflowRunSweeper> logger) : IWorkflowRunSweeper
{
    /// <inheritdoc/>
    public async Task SweepScope(WorkflowScope scope, DateTimeOffset now)
    {
        var runs = await grains.GetGrain<IWorkflowRunIndex>(scope.Value).GetActive();
        foreach (var run in runs)
        {
            try
            {
                await grains.GetGrain<IWorkflowRun>(new WorkflowRunKey(scope, run).ToString()).Tend(now);
            }
            catch (Exception exception)
            {
                logger.RunCouldNotBeTended(exception, run.Value);
            }
        }
    }
}
