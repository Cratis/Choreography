// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Graphs;

/// <summary>
/// What is structurally wrong with a workflow graph.
/// </summary>
public enum WorkflowStructureViolationKind
{
    /// <summary>The graph has no trigger node.</summary>
    NoTrigger = 0,

    /// <summary>The graph has more than one trigger node.</summary>
    MoreThanOneTrigger = 1,

    /// <summary>An edge refers to a node the graph does not have.</summary>
    UnknownNode = 2,

    /// <summary>An edge leaves through an outlet its node does not declare.</summary>
    UndeclaredOutlet = 3,

    /// <summary>A for-each has no join, or more than one.</summary>
    ForEachWithoutSingleJoin = 4,

    /// <summary>A for-each sits inside another for-each's branches.</summary>
    NestedForEachNotSupported = 5,

    /// <summary>A cycle passes through a node that may only be visited once.</summary>
    UnboundedCycle = 6,

    /// <summary>A node's configuration does not describe what its kind needs.</summary>
    InvalidConfiguration = 7,

    /// <summary>Two nodes share an identity.</summary>
    DuplicateNode = 8
}
