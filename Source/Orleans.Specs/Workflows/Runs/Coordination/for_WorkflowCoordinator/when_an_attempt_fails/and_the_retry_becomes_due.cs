// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Graphs;

namespace Cratis.Orleans.Workflows.Runs.Coordination.for_WorkflowCoordinator.when_an_attempt_fails;

public class and_the_retry_becomes_due : given.a_coordinator
{
    static readonly WorkflowNodePolicy _policy = new(3, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(30), 1);

    void Establish()
    {
        Admit([Trigger(), Action("a", _policy)], [Edge("trigger", "next", "a")]);
        Fail(Root("a"));
    }

    void Because() => StartRetriesAt(Now + TimeSpan.FromSeconds(30));

    [Fact] void should_record_only_the_next_attempt_being_scheduled() => Decision.Events.ShouldContainOnly(Recorded<WorkflowNodeScheduled>());
    [Fact] void should_schedule_the_second_attempt() => Recorded<WorkflowNodeScheduled>().Single().Attempt.ShouldEqual(new WorkflowAttempt(2));
    [Fact] void should_run_it() => StatusOf(Root("a")).ShouldEqual(NodeExecutionStatus.Scheduled);
}
