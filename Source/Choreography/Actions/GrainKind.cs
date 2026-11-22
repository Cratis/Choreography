// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Actions;

/// <summary>
/// Defines the kind of grain a <see cref="GrainIdentity"/> identifies.
/// </summary>
public enum GrainKind
{
    /// <summary>
    /// A state machine grain.
    /// </summary>
    StateMachine = 0,

    /// <summary>
    /// A workflow grain - the entry point that admits runs of a workflow.
    /// </summary>
    Workflow = 1,

    /// <summary>
    /// A workflow run grain - one run of a workflow.
    /// </summary>
    WorkflowRun = 2
}
