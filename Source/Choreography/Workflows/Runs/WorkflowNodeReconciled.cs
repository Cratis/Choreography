// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Runs;

/// <summary>
/// A person found out what an ambiguous node execution did and chose its outcome, taking the run forward again. The
/// chosen outcome is recorded right after it, as the success, skip or failure of the execution.
/// </summary>
/// <param name="Execution">The <see cref="NodeExecutionKey"/>.</param>
/// <param name="Attempt">The <see cref="WorkflowAttempt"/> whose outcome was unknown.</param>
/// <param name="Actor">The <see cref="WorkflowActor"/> who chose the outcome.</param>
public record WorkflowNodeReconciled(NodeExecutionKey Execution, WorkflowAttempt Attempt, WorkflowActor Actor);
