// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.StateMachines;

/// <summary>
/// The exception that is thrown when an action of a state fails.
/// </summary>
/// <param name="stateType">The type of state the action belongs to.</param>
/// <param name="phase">The <see cref="StateActionPhase"/> the action was executing in.</param>
/// <param name="reason">Why the action failed.</param>
public class StateActionFailed(Type stateType, StateActionPhase phase, string reason)
    : Exception($"An {phase.ToString().ToLowerInvariant()} action of state '{stateType.FullName}' failed: {reason}")
{
    /// <summary>
    /// Gets the type of state the action belongs to.
    /// </summary>
    public Type StateType { get; } = stateType;

    /// <summary>
    /// Gets the <see cref="StateActionPhase"/> the action was executing in.
    /// </summary>
    public StateActionPhase Phase { get; } = phase;
}
