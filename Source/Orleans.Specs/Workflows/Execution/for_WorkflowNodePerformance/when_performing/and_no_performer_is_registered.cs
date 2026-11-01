// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Runs;

namespace Cratis.Orleans.Workflows.Execution.for_WorkflowNodePerformance.when_performing;

public class and_no_performer_is_registered : given.a_performance
{
    void Establish() => Performer = null;

    Task Because() => Perform();

    [Fact] Task should_report_the_missing_performer() => Run.Received(1).FailAttempt(Execution, Attempt, WorkflowFailureKind.PerformerMissing, Arg.Any<WorkflowFailureReason>());
}
