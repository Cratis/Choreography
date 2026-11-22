// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.StateMachines;

/// <summary>
/// Defines when in the life of a state its actions execute.
/// </summary>
public enum StateActionPhase
{
    /// <summary>
    /// As the state machine enters the state.
    /// </summary>
    Entry = 0,

    /// <summary>
    /// Once the state machine is in the state.
    /// </summary>
    Running = 1,

    /// <summary>
    /// As the state machine leaves the state.
    /// </summary>
    Exit = 2
}
