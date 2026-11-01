// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Graphs;

namespace Cratis.Orleans.Workflows.Runs.Coordination.for_WorkflowCoordinator.when_joining;

public class and_mode_is_any : when_fanning_out.given.a_fan_out
{
    void Establish()
    {
        AdmitFanOut(mode: JoinMode.Any);
        Succeed(Root("map"), output: Targets("csharp", "typescript"));
        Fail(InBranch("publish", "each-target", "typescript"));
    }

    void Because() => Succeed(InBranch("publish", "each-target", "csharp"));

    [Fact] void should_join_through_completed() => Recorded<WorkflowBranchesJoined>().Single().Outlet.ShouldEqual(WellKnownOutlets.Completed);
}
