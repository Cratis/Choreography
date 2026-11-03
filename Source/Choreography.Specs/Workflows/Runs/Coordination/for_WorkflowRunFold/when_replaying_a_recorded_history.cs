// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowRunFold;

/// <summary>
/// A run driven through a fan-out with a retry, then rebuilt from nothing but the events it recorded -
/// the way a grain rebuilds a run after a restart.
/// </summary>
public class when_replaying_a_recorded_history : for_WorkflowCoordinator.when_fanning_out.given.a_fan_out
{
    WorkflowRunState _replayed = null!;

    void Establish()
    {
        AdmitFanOut();
        Succeed(Root("map"), output: Targets("csharp", "typescript"));
        Succeed(InBranch("publish", "each-target", "csharp"));
    }

    void Because() => _replayed = WorkflowRunFold.Replay(Run.Run, History);

    [Fact] void should_have_the_same_executions() => Describe(_replayed).ShouldEqual(Describe(State));
    [Fact] void should_have_the_same_routes() => _replayed.Routes.ShouldContainOnly(State.Routes);
    [Fact] void should_have_the_same_branches() => _replayed.Branches.Keys.ShouldContainOnly(State.Branches.Keys);
    [Fact] void should_have_the_same_last_wave() => _replayed.LastWave.ShouldEqual(State.LastWave);
    [Fact] void should_have_the_same_status() => _replayed.Status.ShouldEqual(State.Status);
    [Fact] void should_still_be_waiting_for_the_other_branch() => _replayed.Executions[InBranch("publish", "each-target", "typescript")].Status.ShouldEqual(NodeExecutionStatus.Scheduled);

    static string Describe(WorkflowRunState state) => string.Join(
        Environment.NewLine,
        state.Executions.Values.Select(execution => $"{execution.Key}:{execution.Status}:{execution.Attempt.Value}:{execution.Outlet.Value}:{execution.Output.Value}:{execution.Job.Value}"));
}
