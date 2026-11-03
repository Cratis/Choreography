// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Adapters;

namespace Cratis.Choreography.Workflows.Execution.for_WorkflowNodePerformance.when_performing;

public class and_the_report_cannot_reach_the_run : given.a_performance
{
    Exception? _exception;

    void Establish()
    {
        Performer!.Perform(Arg.Any<WorkflowNodeInvocation>(), Arg.Any<CancellationToken>()).Returns(WorkflowNodeOutcome.Succeeded("next"));
        Run.CompleteNode(Execution, Attempt, Arg.Any<WorkflowNodeOutcome>()).Returns<Task>(_ => throw new InvalidOperationException("transport unavailable"));
    }

    async Task Because() => _exception = await Cratis.Specifications.Catch.Exception(Perform);

    [Fact] void should_fail_loudly() => _exception.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_report_a_second_business_outcome() => Run.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(Run.CompleteNode)).ShouldEqual(1);
}
