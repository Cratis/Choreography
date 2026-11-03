// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Graphs;

/// <summary>
/// One structural problem with a workflow graph.
/// </summary>
/// <param name="Kind">The <see cref="WorkflowStructureViolationKind"/>.</param>
/// <param name="Node">The <see cref="WorkflowNodeId"/> the problem is at.</param>
/// <param name="Description">What is wrong, for a person to read.</param>
public record WorkflowStructureViolation(WorkflowStructureViolationKind Kind, WorkflowNodeId Node, string Description);
