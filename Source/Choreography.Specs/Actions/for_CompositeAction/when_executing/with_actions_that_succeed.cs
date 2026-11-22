// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Actions.for_CompositeAction.when_executing;

public class with_actions_that_succeed : given.an_action_context
{
    readonly List<string> _log = [];
    ActionResult _result;

    async Task Because()
    {
        var composite = new CompositeAction(
            CodeAction.Run(_ => _log.Add("first")),
            new CodeAction((_, _) =>
            {
                _log.Add("second");
                return Task.FromResult(ActionResult.Succeeded("""{"last":true}"""));
            }));

        _result = await composite.Execute(_context, CancellationToken.None);
    }

    [Fact] void should_execute_the_actions_in_order() => _log.ShouldEqual(["first", "second"]);
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_output_what_the_last_action_output() => _result.Output.ShouldEqual("""{"last":true}""");
}
