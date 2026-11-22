// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Actions.for_CompositeAction.when_executing;

public class with_an_action_that_fails : given.an_action_context
{
    readonly List<string> _log = [];
    ActionResult _result;

    async Task Because()
    {
        var composite = new CompositeAction(
            new CompositeAction(CodeAction.Run(_ => _log.Add("first"))),
            new CodeAction((_, _) => Task.FromResult(ActionResult.Failed("broken"))),
            CodeAction.Run(_ => _log.Add("third")));

        _result = await composite.Execute(_context, CancellationToken.None);
    }

    [Fact] void should_stop_at_the_failing_action() => _log.ShouldEqual(["first"]);
    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_say_which_action_failed_and_why() => _result.Reason.ShouldContain("Action 2 of 3");
    [Fact] void should_carry_the_reason_of_the_failing_action() => _result.Reason.ShouldContain("broken");
}
