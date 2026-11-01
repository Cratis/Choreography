// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Contracts;

namespace Cratis.Orleans.Workflows.Runs.Coordination;

/// <summary>
/// Where a branch came from - the for-each that fanned it out, and its item.
/// </summary>
/// <param name="ForEach">The <see cref="NodeExecutionKey"/> of the for-each.</param>
/// <param name="Item">The item, as <see cref="WorkflowData"/>.</param>
public record WorkflowBranchOrigin(NodeExecutionKey ForEach, WorkflowData Item);
