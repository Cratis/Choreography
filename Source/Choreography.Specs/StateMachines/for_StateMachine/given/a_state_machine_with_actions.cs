// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Actions;

namespace Cratis.Choreography.StateMachines.for_StateMachine.given;

public abstract class a_state_machine_with_actions : a_state_machine
{
    protected List<string> _log = [];

    protected override IEnumerable<IState<StateMachineStateForTesting>> CreateStates() =>
    [
        new StateWithActions(Log("A.entry"), Log("A.running"), Log("A.exit")),
        new OtherStateWithActions(Log("B.entry"), Log("B.running"), Log("B.exit"))
    ];

    protected IEnumerable<IAction> Log(string entry) => [CodeAction.Run(_ => _log.Add(entry))];
}
