// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.StateMachines;

namespace Cratis.Choreography.Chronicle.StateMachines.Misfits;

public class StateMachineWithoutProjection : EventSourcedStateMachine<UnprojectedModel>
{
    public override IImmutableList<IState<UnprojectedModel>> CreateStates() => [];
}
