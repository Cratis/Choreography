// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Sweeping;

/// <summary>
/// Defines a system that takes the runs of a scope forward when time has made something due.
/// </summary>
public interface IWorkflowRunSweeper
{
    /// <summary>
    /// Asks every run of a scope that is not over to act on what has become due - waits that have expired, retries
    /// whose time has come, work whose wave job is gone.
    /// </summary>
    /// <param name="scope">The <see cref="WorkflowScope"/>.</param>
    /// <param name="now">The time.</param>
    /// <returns>Awaitable task.</returns>
    Task SweepScope(WorkflowScope scope, DateTimeOffset now);
}
