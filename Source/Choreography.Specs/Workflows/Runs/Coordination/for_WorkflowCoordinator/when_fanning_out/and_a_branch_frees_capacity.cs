// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator.when_fanning_out;

public class and_a_branch_frees_capacity : given.a_fan_out
{
    void Establish()
    {
        AdmitFanOut(maxParallelism: 2);
        Succeed(Root("map"), output: Targets("csharp", "typescript", "python"));
    }

    void Because() => Succeed(InBranch("publish", "each-target", "csharp"));

    [Fact] void should_schedule_the_waiting_branch() => ScheduledNow.ShouldContainOnly(InBranch("publish", "each-target", "python"));
}
