// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Runs.Coordination;

/// <summary>
/// One fan-out of a for-each, as the run's history recorded it.
/// </summary>
/// <param name="ForEach">The <see cref="NodeExecutionKey"/> of the for-each.</param>
/// <param name="Branches">The <see cref="WorkflowBranch">branches</see>.</param>
/// <param name="IsJoined">Whether its join has gathered the branches - after which nothing new starts in them.</param>
public record WorkflowFanOut(NodeExecutionKey ForEach, IReadOnlyList<WorkflowBranch> Branches, bool IsJoined);
