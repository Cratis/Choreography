// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Adapters;

namespace Cratis.Orleans.Workflows.Execution.for_WorkflowNodePerformance.when_performing;

public class and_the_performer_succeeds : given.a_performance
{
    static readonly WorkflowNodeOutcome _outcome = WorkflowNodeOutcome.Succeeded("next", """{"published":true}""");
    WorkflowNodeInvocation? _invocation;

    void Establish() => Performer!
        .Perform(Arg.Do<WorkflowNodeInvocation>(invocation => _invocation = invocation), Arg.Any<CancellationToken>())
        .Returns(_outcome);

    Task Because() => Perform();

    [Fact] Task should_report_the_outcome() => Run.Received(1).CompleteNode(Execution, Attempt, _outcome);
    [Fact] void should_pass_the_idempotency_key() => _invocation!.IdempotencyKey.ShouldEqual(new WorkflowIdempotencyKey("idempotency-1"));
    [Fact] void should_pass_the_evidence() => _invocation!.Evidence.ShouldContainOnly(new Contracts.EvidenceReference("evidence-1"));
    [Fact] void should_pass_the_tenant_qualified_run() => _invocation!.Run.ToString().ShouldEqual("tenant/run-1");
}
