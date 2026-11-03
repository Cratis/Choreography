// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Graphs;

/// <summary>
/// What kind of node a workflow node is - which decides how the engine runs it.
/// </summary>
public enum WorkflowNodeKind
{
    /// <summary>The node a run starts from; completed by the engine with the trigger's payload.</summary>
    Trigger = 0,

    /// <summary>A node that does something, performed by an adapter in a job step.</summary>
    Action = 1,

    /// <summary>A node that chooses an outlet, performed by a decision provider in a job step.</summary>
    Decision = 2,

    /// <summary>A node that waits for a signal or a timer without holding a worker.</summary>
    Wait = 3,

    /// <summary>A node that waits for a person to approve or reject.</summary>
    Approval = 4,

    /// <summary>A node that fans out one branch per item of a list.</summary>
    ForEach = 5,

    /// <summary>A node that gathers the branches of a for-each back together.</summary>
    Join = 6
}
