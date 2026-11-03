// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Chronicle.Workflows;

/// <summary>
/// The exception that is thrown when a workflow is started for a state whose machine is not a grain.
/// </summary>
/// <param name="type">The type of the machine.</param>
public class StateMachineIsNotAGrain(Type type)
    : Exception($"State machine '{type}' is not a grain, so a workflow cannot be started for it");
