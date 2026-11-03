// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator.when_composing_input;

public class with_oversized_generated_input : given.a_coordinator
{
    string _output = null!;

    void Establish()
    {
        _output = "{\"value\":\"" + new string('x', 17000) + "\"}";
        Admit([Trigger(), Action("a"), Action("b"), Action("c")],
            [Edge("trigger", "next", "a"), Edge("a", "next", "b"), Edge("b", "next", "c")]);
        Succeed(Root("a"), output: _output);
    }

    void Because() => Succeed(Root("b"), output: _output);

    [Fact] void should_not_schedule_unbounded_input() => Decision.ToSchedule.ShouldBeEmpty();
    [Fact] void should_record_invalid_output() => Recorded<WorkflowNodeFailed>().Single().Kind.ShouldEqual(WorkflowFailureKind.InvalidOutput);
}
