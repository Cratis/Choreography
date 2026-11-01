// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Runs.Coordination.for_WorkflowCoordinator.when_fanning_out;

public class and_item_keys_repeat : given.a_fan_out
{
    void Establish() => AdmitFanOut();

    void Because() => Succeed(Root("map"), output: Targets("csharp", "csharp"));

    [Fact] void should_fail_the_for_each_as_invalid_output() => Recorded<WorkflowNodeFailed>().Single().ShouldEqual(Recorded<WorkflowNodeFailed>().Single() with { Execution = Root("each-target"), Kind = WorkflowFailureKind.InvalidOutput });
    [Fact] void should_not_fan_out() => Recorded<WorkflowBranchesFannedOut>().ShouldBeEmpty();
    [Fact] void should_fail_the_run() => Recorded<WorkflowRunFailed>().ShouldNotBeEmpty();
}
