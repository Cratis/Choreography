// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator;

public class when_an_expired_wait_is_resolved : given.a_coordinator
{
    void Establish()
    {
        Admit([Trigger(), SignalWait("wait", TimeSpan.FromMinutes(1))], [Edge("trigger", "next", "wait")]);
        ExpireWaitsAt(Now + TimeSpan.FromMinutes(2));
    }

    void Because() => ResolveWaitOf(Root("wait"), "resumed");

    [Fact] void should_record_nothing() => Decision.Events.ShouldBeEmpty();
    [Fact] void should_keep_the_expired_outlet() => State.Executions[Root("wait")].Outlet.Value.ShouldEqual("expired");
}
