// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Runs.Coordination.for_WorkflowCoordinator.when_fanning_out;

public class and_max_parallelism_is_two : given.a_fan_out
{
    void Establish() => AdmitFanOut(maxParallelism: 2);

    void Because() => Succeed(Root("map"), output: Targets("csharp", "typescript", "python"));

    [Fact] void should_fan_out_one_branch_per_item() => Recorded<WorkflowBranchesFannedOut>().Single().Branches.Select(branch => branch.Key.Value).ShouldContainOnly("each-target//1:csharp", "each-target//1:typescript", "each-target//1:python");
    [Fact] void should_schedule_only_two_branches() => ScheduledNow.ShouldContainOnly(InBranch("publish", "each-target", "csharp"), InBranch("publish", "each-target", "typescript"));
    [Fact] void should_give_each_branch_its_item() => Decision.ToSchedule.First().Input.Contains("\"language\":\"csharp\"", StringComparison.Ordinal).ShouldBeTrue();
}
