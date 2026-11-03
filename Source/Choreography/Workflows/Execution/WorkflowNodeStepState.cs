// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage.Jobs;

namespace Cratis.Choreography.Workflows.Execution;

/// <summary>
/// The persisted state of a <see cref="WorkflowNodeStep"/> - what to perform, carried here so it
/// survives a restart between the step being prepared and it running.
/// </summary>
public class WorkflowNodeStepState : JobStepState
{
    /// <summary>
    /// Gets or sets the <see cref="WorkflowWaveItem"/> the step performs.
    /// </summary>
    public WorkflowWaveItem? Item { get; set; }
}
