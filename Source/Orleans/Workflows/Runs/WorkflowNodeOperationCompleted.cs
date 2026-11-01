// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Adapters;
using Cratis.Orleans.Workflows.Contracts;

namespace Cratis.Orleans.Workflows.Runs;

/// <summary>
/// One operation of a staged node completed - the stage barrier the next operation reads from.
/// </summary>
/// <param name="Execution">The <see cref="NodeExecutionKey"/>.</param>
/// <param name="Attempt">The <see cref="WorkflowAttempt"/>.</param>
/// <param name="Operation">The <see cref="WorkflowOperationName"/>.</param>
/// <param name="Output">What the operation produced, as <see cref="WorkflowData"/>.</param>
public record WorkflowNodeOperationCompleted(NodeExecutionKey Execution, WorkflowAttempt Attempt, WorkflowOperationName Operation, WorkflowData Output);
