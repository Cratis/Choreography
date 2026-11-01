// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Graphs;

namespace Cratis.Orleans.Workflows.Runs;

/// <summary>
/// A node execution failed for good.
/// </summary>
/// <param name="Execution">The <see cref="NodeExecutionKey"/>.</param>
/// <param name="Attempt">The <see cref="WorkflowAttempt"/> it failed on.</param>
/// <param name="Kind">The <see cref="WorkflowFailureKind"/>.</param>
/// <param name="Reason">Why, as a <see cref="WorkflowFailureReason"/>.</param>
/// <param name="RoutedTo">The outlet the failure was routed through - <see cref="OutletName.None"/> when nothing handles it.</param>
public record WorkflowNodeFailed(NodeExecutionKey Execution, WorkflowAttempt Attempt, WorkflowFailureKind Kind, WorkflowFailureReason Reason, OutletName RoutedTo);
