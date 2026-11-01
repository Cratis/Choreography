// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Runs.Coordination.for_WorkflowCoordinator.when_joining;

public class with_oversized_aggregate_output : when_fanning_out.given.a_fan_out
{
    string _output = null!;

    void Establish()
    {
        _output = "{\"value\":\"" + new string('x', 17000) + "\"}";
        AdmitFanOut();
        Succeed(Root("map"), output: Targets("one", "two"));
        Succeed(InBranch("publish", "each-target", "one"), output: _output);
    }

    void Because() => Succeed(InBranch("publish", "each-target", "two"), output: _output);

    [Fact] void should_not_record_oversized_success() => Recorded<WorkflowBranchesJoined>().ShouldBeEmpty();
    [Fact] void should_reject_the_join_data() => Recorded<WorkflowNodeFailed>().Single().Kind.ShouldEqual(WorkflowFailureKind.InvalidOutput);
    [Fact] void should_end_as_a_failure() => State.Status.ShouldEqual(WorkflowRunStatus.Failed);
}
