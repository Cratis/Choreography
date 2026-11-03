// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator.when_an_attempt_fails;

public class and_the_retry_is_checked_before_it_is_due : given.a_coordinator
{
    static readonly WorkflowNodePolicy _policy = new(3, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(30), 1);

    void Establish()
    {
        Admit([Trigger(), Action("a", _policy)], [Edge("trigger", "next", "a")]);
        Fail(Root("a"));
    }

    void Because() => StartRetriesAt(Now + TimeSpan.FromSeconds(29));

    [Fact] void should_record_nothing() => Decision.Events.ShouldBeEmpty();
    [Fact] void should_keep_the_retry_pending() => StatusOf(Root("a")).ShouldEqual(NodeExecutionStatus.RetryPending);
}
