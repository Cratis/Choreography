// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Actions;

namespace Cratis.Choreography.StateMachines;

/// <summary>
/// Another state that executes the actions it is given.
/// </summary>
/// <param name="entry">The actions to execute on entry.</param>
/// <param name="running">The actions to execute while running.</param>
/// <param name="exit">The actions to execute on exit.</param>
public class OtherStateWithActions(IEnumerable<IAction>? entry = default, IEnumerable<IAction>? running = default, IEnumerable<IAction>? exit = default) : StateWithActions(entry, running, exit);
