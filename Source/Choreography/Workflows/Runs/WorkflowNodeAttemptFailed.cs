// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Runs;

/// <summary>
/// One attempt at a node execution failed.
/// </summary>
/// <remarks>
/// When the failure is retryable and the node's policy has attempts left, nothing else is recorded: the
/// next attempt is due the node's retry delay after this was recorded, and becomes a fact of its own -
/// <see cref="WorkflowNodeScheduled"/> - only when it actually starts. Otherwise <see cref="WorkflowNodeFailed"/> follows.
/// </remarks>
/// <param name="Execution">The <see cref="NodeExecutionKey"/>.</param>
/// <param name="Attempt">The <see cref="WorkflowAttempt"/> that failed.</param>
/// <param name="Kind">The <see cref="WorkflowFailureKind"/>.</param>
/// <param name="Reason">Why, as a <see cref="WorkflowFailureReason"/>.</param>
public record WorkflowNodeAttemptFailed(NodeExecutionKey Execution, WorkflowAttempt Attempt, WorkflowFailureKind Kind, WorkflowFailureReason Reason);
