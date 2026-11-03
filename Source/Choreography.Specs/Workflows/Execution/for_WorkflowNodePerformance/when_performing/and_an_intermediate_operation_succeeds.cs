// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Runs;

namespace Cratis.Choreography.Workflows.Execution.for_WorkflowNodePerformance.when_performing;

public class and_an_intermediate_operation_succeeds : given.a_performance
{
    IStagedWorkflowNodePerformer _staged = null!;

    void Establish()
    {
        _staged = Substitute.For<IStagedWorkflowNodePerformer>();
        _staged.Operations.Returns([new WorkflowOperationName("register"), new WorkflowOperationName("start")]);
        _staged.PerformOperation(Arg.Any<WorkflowNodeInvocation>(), new WorkflowOperationName("register"), WorkflowData.Empty, Arg.Any<CancellationToken>())
            .Returns(WorkflowNodeOutcome.Succeeded("next", """{"registration":"r-1"}"""));
        Performer = _staged;
        Item = Item with { Operation = "register" };
    }

    Task Because() => Perform();

    [Fact] Task should_record_the_operation() => Run.Received(1).CompleteOperation(Execution, Attempt, new WorkflowOperationName("register"), new WorkflowData("""{"registration":"r-1"}"""));
    [Fact] Task should_not_complete_the_node() => Run.DidNotReceive().CompleteNode(Arg.Any<NodeExecutionKey>(), Arg.Any<WorkflowAttempt>(), Arg.Any<WorkflowNodeOutcome>());
}
