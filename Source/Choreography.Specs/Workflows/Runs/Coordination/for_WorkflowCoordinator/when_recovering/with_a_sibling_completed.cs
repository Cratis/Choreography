// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator.when_recovering;

public class with_a_sibling_completed : given.a_coordinator
{
    string _input = null!;
    string _key = null!;

    void Establish()
    {
        Admit([Trigger(), Action("a"), Action("b"), Action("c")],
            [Edge("trigger", "next", "a"), Edge("trigger", "next", "b"), Edge("a", "next", "c")]);
        Succeed(Root("a"), output: "{\"a\":1}");
        _input = Decision.ToSchedule.Single().Input;
        _key = Decision.ToSchedule.Single().IdempotencyKey;
        Succeed(Root("b"), output: "{\"newSibling\":true}");
        State = WorkflowRunFold.Replay(Run.Run, History);
    }

    void Because() => Apply(WorkflowCoordinator.Resume(Run, State, [], Now));

    [Fact] void should_reuse_the_recorded_input() => Decision.ToSchedule.Single().Input.ShouldEqual(_input);
    [Fact] void should_reuse_the_effect_key() => Decision.ToSchedule.Single().IdempotencyKey.ShouldEqual(_key);
    [Fact] void should_not_pin_a_second_input() => Recorded<WorkflowNodeInputPinned>().ShouldBeEmpty();
}
