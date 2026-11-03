// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Runs;

/// <summary>
/// A for-each fanned out - one branch per item.
/// </summary>
/// <param name="ForEach">The <see cref="NodeExecutionKey"/> of the for-each.</param>
/// <param name="Branches">The <see cref="WorkflowBranch">branches</see>.</param>
public record WorkflowBranchesFannedOut(NodeExecutionKey ForEach, IEnumerable<WorkflowBranch> Branches);
