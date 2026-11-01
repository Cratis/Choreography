// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Waits;

namespace Cratis.Orleans.Workflows.Runs.Coordination;

/// <summary>
/// A wait the run's history says is open.
/// </summary>
/// <param name="Wait">The <see cref="WorkflowWaitId"/>.</param>
/// <param name="Execution">The <see cref="NodeExecutionKey"/> that waits.</param>
/// <param name="Kind">The <see cref="WaitKind"/>.</param>
/// <param name="Deadline">The <see cref="WaitDeadline"/>.</param>
public record WorkflowOpenWait(WorkflowWaitId Wait, NodeExecutionKey Execution, WaitKind Kind, WaitDeadline Deadline);
