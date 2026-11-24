// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.StateMachines.for_StateMachine.when_activating;

public class and_the_activation_is_cancelled_while_entering_the_state : given.a_state_machine
{
    readonly CancellationTokenSource _cancellation = new();
    bool _armed;
    Task _activation;

    protected override Type? InitialState => typeof(StateThatSupportsTransitioningFrom);

    protected override IEnumerable<IState<StateMachineStateForTesting>> CreateStates() =>
    [
        new StateThatSupportsTransitioningFrom { OnEnterCalled = _ => { if (_armed) _cancellation.Cancel(); } }
    ];

    async Task Because()
    {
        // The test kit activates the grain on creation; clear that, then activate with a token that is cancelled mid-transition.
        StateMachine.OnAfterEnteringStates.Clear();
        _armed = true;
        _activation = StateMachine.OnActivateAsync(_cancellation.Token);
        await Catch.Exception(() => _activation);
    }

    [Fact] void should_end_the_activation_as_cancelled() => _activation.IsCanceled.ShouldBeTrue();
    [Fact] void should_not_report_the_activation_as_faulted() => _activation.IsFaulted.ShouldBeFalse();
    [Fact] void should_not_complete_entering_the_state() => StateMachine.OnAfterEnteringStates.ShouldBeEmpty();
}
