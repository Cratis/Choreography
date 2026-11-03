// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator.when_joining;

public class and_one_branch_failed_in_all_mode : when_fanning_out.given.a_fan_out
{
    void Establish()
    {
        AdmitFanOut();
        Succeed(Root("map"), output: Targets("csharp", "typescript"));
        Succeed(InBranch("publish", "each-target", "csharp"), output: """{"published":"1.0.0"}""");
    }

    void Because() => Fail(InBranch("publish", "each-target", "typescript"));

    WorkflowBranchesJoined Joined => Recorded<WorkflowBranchesJoined>().Single();

    [Fact] void should_join_through_partial() => Joined.Outlet.ShouldEqual(WellKnownOutlets.Partial);
    [Fact] void should_report_the_succeeded_target() => Joined.Results.Value.Contains("\"each-target//1:csharp\":{\"key\":\"csharp\",\"status\":\"succeeded\"", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_report_what_the_succeeded_target_produced() => Joined.Results.Value.Contains("1.0.0", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_report_the_failed_target() => Joined.Results.Value.Contains("\"each-target//1:typescript\":{\"key\":\"typescript\",\"status\":\"failed\"", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_fail_the_run_for_a_failed_branch() => Recorded<WorkflowRunSucceeded>().ShouldNotBeEmpty();
}
