// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Orleans.Workflows.Sweeping;

/// <summary>
/// Represents the background service that sweeps every known scope on an interval.
/// </summary>
/// <param name="scopes">The <see cref="WorkflowScopes"/>.</param>
/// <param name="sweeper">The <see cref="IWorkflowRunSweeper"/>.</param>
/// <param name="options">The <see cref="WorkflowsOptions"/>.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/>.</param>
/// <param name="logger">The logger.</param>
public class WorkflowRunSweepService(
    WorkflowScopes scopes,
    IWorkflowRunSweeper sweeper,
    IOptions<WorkflowsOptions> options,
    TimeProvider timeProvider,
    ILogger<WorkflowRunSweepService> logger) : BackgroundService
{
    /// <summary>
    /// Sweeps every known scope once.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>Awaitable task.</returns>
    public async Task SweepOnce(CancellationToken cancellationToken)
    {
        foreach (var scope in await scopes.GetAll(cancellationToken))
        {
            await sweeper.SweepScope(scope, timeProvider.GetUtcNow());
        }
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Sweep)
        {
            return;
        }

        try
        {
            await Task.Delay(options.Value.SweepStartupDelay, timeProvider, stoppingToken);
            using var timer = new PeriodicTimer(options.Value.SweepInterval, timeProvider);
            do
            {
                try
                {
                    await SweepOnce(stoppingToken);
                }
                catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
                {
                    logger.SweepFailed(exception);
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // The silo is stopping.
        }
    }
}
