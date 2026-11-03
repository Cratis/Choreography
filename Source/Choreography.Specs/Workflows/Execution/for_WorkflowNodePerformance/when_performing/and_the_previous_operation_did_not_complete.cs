// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Runs;

namespace Cratis.Choreography.Workflows.Execution.for_WorkflowNodePerformance.when_performing;

public class and_the_previous_operation_did_not_complete : given.a_performance
{
    IStagedWorkflowNodePerformer _staged = null!;

    void Establish()
    {
        _staged = Substitute.For<IStagedWorkflowNodePerformer>();
        _staged.Operations.Returns([new WorkflowOperationName("register"), new WorkflowOperationName("start")]);
        Performer = _staged;
        Item = Item with { Operation = "start" };
        Run.GetOperationOutput(Execution, Attempt, new WorkflowOperationName("register")).Returns(WorkflowData.Empty);
    }

    Task Because() => Perform();

    [Fact] Task should_not_perform_the_operation() => _staged.DidNotReceive().PerformOperation(Arg.Any<WorkflowNodeInvocation>(), Arg.Any<WorkflowOperationName>(), Arg.Any<WorkflowData>(), Arg.Any<CancellationToken>());
    [Fact] Task should_report_nothing() => Run.DidNotReceive().CompleteNode(Arg.Any<NodeExecutionKey>(), Arg.Any<WorkflowAttempt>(), Arg.Any<WorkflowNodeOutcome>());
}
