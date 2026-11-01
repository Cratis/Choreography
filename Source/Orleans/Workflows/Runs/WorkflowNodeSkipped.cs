// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Runs;

/// <summary>
/// A node's performer chose not to do anything - which is not a failure.
/// </summary>
/// <param name="Execution">The <see cref="NodeExecutionKey"/>.</param>
/// <param name="Reason">Why, as a <see cref="WorkflowFailureReason"/>.</param>
public record WorkflowNodeSkipped(NodeExecutionKey Execution, WorkflowFailureReason Reason);
