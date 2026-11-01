// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Runs;

/// <summary>
/// The run finished with nodes that nothing led to.
/// </summary>
/// <param name="Executions">The <see cref="NodeExecutionKey">executions</see> that were never reached.</param>
public record WorkflowNodesUnreachable(IEnumerable<NodeExecutionKey> Executions);
