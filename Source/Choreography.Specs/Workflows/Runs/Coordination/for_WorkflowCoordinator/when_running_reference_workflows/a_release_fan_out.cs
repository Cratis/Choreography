// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator.when_running_reference_workflows;

public class a_release_fan_out : given.reference_capabilities
{
    readonly staged_publish _publish = new();

    void Establish()
    {
        Register("route", _ => WorkflowNodeOutcome.Succeeded("release"));
        Register("map", _ => WorkflowNodeOutcome.Succeeded("next", "{\"targets\":[{\"key\":\"first\"},{\"key\":\"second\"}]}"));
        Admit(
            [Trigger(), Decider("route", "release"), Action("map"), ForEach("targets", "map", "targets"), Action("release", WorkflowNodePolicy.Default with { MaxAttempts = 2 }), Join("join", "targets")],
            [Edge("trigger", "next", "route"), Edge("route", "release", "map"), Edge("map", "next", "targets"), Edge("targets", "each", "release"), Edge("release", "next", "join")]);
    }

    async Task Because()
    {
        await Drive(Root("route"));
        await Drive(Root("map"));
        await DriveStaged(InBranch("release", "targets", "first"), _publish);
        await DriveStaged(InBranch("release", "targets", "second"), _publish);
        StartRetriesAt(Now + TimeSpan.FromMinutes(1));
        await DriveStaged(InBranch("release", "targets", "second"), _publish);
    }

    [Fact] void should_join_through_partial() => Recorded<WorkflowBranchesJoined>().Single().Outlet.ShouldEqual(WellKnownOutlets.Partial);
    [Fact] void should_not_repeat_the_succeeded_target() => _publish.Calls[InBranch("release", "targets", "first").Branch].ShouldEqual(2);
    [Fact] void should_retry_only_the_failed_target() => _publish.Calls[InBranch("release", "targets", "second").Branch].ShouldEqual(4);
    [Fact] void should_record_each_completed_registration() => History.OfType<WorkflowNodeOperationCompleted>().Count().ShouldEqual(3);
    [Fact] void should_finish_the_run() => State.Status.ShouldEqual(WorkflowRunStatus.Succeeded);
}
