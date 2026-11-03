// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Runs;

/// <summary>
/// Whether a node execution's effect happened is unknown. It is never retried automatically -
/// a person has to find out.
/// </summary>
/// <param name="Execution">The <see cref="NodeExecutionKey"/>.</param>
/// <param name="Attempt">The <see cref="WorkflowAttempt"/>.</param>
/// <param name="Reason">Why, as a <see cref="WorkflowFailureReason"/>.</param>
public record WorkflowNodeOutcomeAmbiguous(NodeExecutionKey Execution, WorkflowAttempt Attempt, WorkflowFailureReason Reason);
