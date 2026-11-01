// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Runs;

namespace Cratis.Orleans.Chronicle.Workflows.Listing;

/// <summary>
/// A node execution of a run, and where it stands.
/// </summary>
/// <param name="Execution">The execution.</param>
/// <param name="Status">Its status.</param>
public record WorkflowRunExecution(NodeExecutionKey Execution, NodeExecutionStatus Status)
{
    /// <summary>
    /// Gets the wave job that holds the execution while it is scheduled.
    /// </summary>
    public WorkflowJobReference? Job { get; init; }
}
