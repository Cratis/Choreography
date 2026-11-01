// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Runs.Coordination.for_WorkflowCoordinator;

public class when_a_resolved_wait_is_resolved_again : given.a_coordinator
{
    void Establish()
    {
        Admit([Trigger(), SignalWait("wait", TimeSpan.Zero)], [Edge("trigger", "next", "wait")]);
        ResolveWaitOf(Root("wait"), "resumed");
    }

    void Because() => ResolveWaitOf(Root("wait"), "resumed");

    [Fact] void should_record_nothing() => Decision.Events.ShouldBeEmpty();
}
