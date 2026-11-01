// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Runs;

/// <summary>
/// An attempt at a node execution was scheduled into a wave job.
/// </summary>
/// <param name="Execution">The <see cref="NodeExecutionKey"/>.</param>
/// <param name="Attempt">The <see cref="WorkflowAttempt"/>.</param>
/// <param name="Job">The <see cref="WorkflowJobReference"/> of the wave job it runs in.</param>
/// <param name="Wave">The wave number within the run.</param>
public record WorkflowNodeScheduled(NodeExecutionKey Execution, WorkflowAttempt Attempt, WorkflowJobReference Job, WorkflowWave Wave);
