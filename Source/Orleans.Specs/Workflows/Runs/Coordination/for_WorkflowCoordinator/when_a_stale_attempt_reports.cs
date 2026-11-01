// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Adapters;
using Cratis.Orleans.Workflows.Graphs;

namespace Cratis.Orleans.Workflows.Runs.Coordination.for_WorkflowCoordinator;

public class when_a_stale_attempt_reports : given.a_coordinator
{
    void Establish()
    {
        Admit([Trigger(), Action("a", new WorkflowNodePolicy(3, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(30), 1))], [Edge("trigger", "next", "a")]);
        Fail(Root("a"));
        StartRetriesAt(Now + TimeSpan.FromMinutes(1));
    }

    void Because() => Report(Root("a"), WorkflowAttempt.First, WorkflowNodeOutcome.Succeeded("next"));

    [Fact] void should_record_nothing() => Decision.Events.ShouldBeEmpty();
    [Fact] void should_still_expect_the_second_attempt() => AttemptOf(Root("a")).ShouldEqual(new WorkflowAttempt(2));
}
