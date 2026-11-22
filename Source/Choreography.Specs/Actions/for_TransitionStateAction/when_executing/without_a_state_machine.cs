// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Actions.for_TransitionStateAction.when_executing;

public class without_a_state_machine : given.an_action_context
{
    ActionResult _result;

    async Task Because() => _result = await TransitionStateAction.To<string>().Execute(_context, CancellationToken.None);

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
}
