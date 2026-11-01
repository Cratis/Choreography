// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Orleans.Workflows;
using Cratis.Orleans.Workflows.Contracts;
using Cratis.Orleans.Workflows.Runs;

namespace Cratis.Orleans.Chronicle.Workflows.Listing.for_WorkflowRun.when_projecting;

public class with_operation_activity : Specification
{
    readonly WorkflowRunReducer _reducer = new();
    readonly DateTimeOffset _activityAt = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);
    WorkflowRun _current = null!;
    WorkflowRun _result = null!;

    void Establish() => _current = _reducer.On(
        given.an_admission(WorkflowId.New()),
        null,
        EventContext.Empty with { EventSourceId = Guid.NewGuid().ToString(), Occurred = _activityAt.AddHours(-1) });

    void Because() => _result = _reducer.On(
        new WorkflowNodeOperationCompleted(NodeExecutionKey.RootOf("action"), WorkflowAttempt.First, "register", WorkflowData.Empty),
        _current,
        EventContext.Empty with { Occurred = _activityAt })!;

    [Fact] void should_advance_last_activity_to_the_operation_time() => _result.LastActivityAt.ShouldEqual(_activityAt);
}
