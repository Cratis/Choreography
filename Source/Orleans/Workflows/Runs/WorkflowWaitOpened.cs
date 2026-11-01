// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Contracts;
using Cratis.Orleans.Workflows.Waits;

namespace Cratis.Orleans.Workflows.Runs;

/// <summary>
/// A node execution started waiting - for a signal, a timer or a person - without holding a worker.
/// </summary>
/// <param name="Wait">The <see cref="WorkflowWaitId"/>.</param>
/// <param name="Execution">The <see cref="NodeExecutionKey"/> that waits.</param>
/// <param name="Kind">The <see cref="WaitKind"/>.</param>
/// <param name="Correlation">The <see cref="WorkflowCorrelationKey"/> a signal is matched on.</param>
/// <param name="Signal">The <see cref="WorkflowContractReference"/> of the signal it accepts.</param>
/// <param name="Deadline">The <see cref="WaitDeadline"/>.</param>
public record WorkflowWaitOpened(WorkflowWaitId Wait, NodeExecutionKey Execution, WaitKind Kind, WorkflowCorrelationKey Correlation, WorkflowContractReference Signal, WaitDeadline Deadline);
