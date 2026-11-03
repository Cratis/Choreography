// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator.when_joining;

public class with_an_early_arrival_and_running_sibling : given.a_forked_branch
{
    void Because() => Apply(WorkflowCoordinator.Resume(Run, State, State.Executions.Values.Select(item => item.Job), Now));

    [Fact] void should_not_join_yet() => History.OfType<WorkflowBranchesJoined>().ShouldBeEmpty();
    [Fact] void should_keep_the_run_active() => State.IsOver.ShouldBeFalse();
}
