// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Triggers;

/// <summary>
/// What started a workflow run.
/// </summary>
public enum WorkflowTriggerKind
{
    /// <summary>A person started it.</summary>
    Manual = 0,

    /// <summary>A signal from outside started it.</summary>
    Signal = 1,

    /// <summary>A subject reaching a stage - a state machine entering a state - started it.</summary>
    StageHook = 2,

    /// <summary>A schedule started it.</summary>
    Schedule = 3
}
