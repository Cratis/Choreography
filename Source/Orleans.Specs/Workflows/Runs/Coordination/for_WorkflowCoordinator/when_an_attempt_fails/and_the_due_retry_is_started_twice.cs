// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Graphs;

namespace Cratis.Orleans.Workflows.Runs.Coordination.for_WorkflowCoordinator.when_an_attempt_fails;

public class and_the_due_retry_is_started_twice : given.a_coordinator
{
    static readonly WorkflowNodePolicy _policy = new(3, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(30), 1);

    void Establish()
    {
        Admit([Trigger(), Action("a", _policy)], [Edge("trigger", "next", "a")]);
        Fail(Root("a"));
        StartRetriesAt(Now + TimeSpan.FromMinutes(1));
    }

    void Because() => StartRetriesAt(Now + TimeSpan.FromMinutes(1));

    [Fact] void should_record_nothing() => Decision.Events.ShouldBeEmpty();
    [Fact] void should_have_scheduled_the_second_attempt_once() => History.OfType<WorkflowNodeScheduled>().Count(scheduled => scheduled.Attempt == new WorkflowAttempt(2)).ShouldEqual(1);
}
