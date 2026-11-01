// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Runs.Coordination.for_WorkflowCoordinator;

public class when_an_approval_is_approved : given.a_coordinator
{
    void Establish() => Admit([Trigger(), Approval("approve", TimeSpan.Zero), Action("yes"), Action("no")], [Edge("trigger", "next", "approve"), Edge("approve", "approved", "yes"), Edge("approve", "rejected", "no")]);

    void Because() => ResolveWaitOf(Root("approve"), "approved");

    [Fact] void should_select_only_the_answered_outlet() => ScheduledNow.ShouldContainOnly(Root("yes"));
}
