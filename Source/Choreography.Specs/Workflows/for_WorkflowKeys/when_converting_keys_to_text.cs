// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.for_WorkflowKeys;

/// <summary>
/// The text of a grain key is what silos of different releases address each other's grains by.
/// </summary>
public class when_converting_keys_to_text : Specification
{
    WorkflowRunKey _run = null!;
    WorkflowKey _workflow = null!;
    WorkflowRunKey _scopeWithSeparator = null!;

    void Because()
    {
        _run = WorkflowRunKey.Parse("tenant-1/run-1");
        _workflow = WorkflowKey.Parse("tenant-1/workflow-1");
        _scopeWithSeparator = WorkflowRunKey.Parse("store/tenant-1/run-1");
    }

    [Fact] void should_write_a_run_key_as_scope_slash_run() => new WorkflowRunKey(new WorkflowScope("tenant-1"), new WorkflowRunId("run-1")).ToString().ShouldEqual("tenant-1/run-1");
    [Fact] void should_write_a_workflow_key_as_scope_slash_workflow() => new WorkflowKey(new WorkflowScope("tenant-1"), new WorkflowId("workflow-1")).ToString().ShouldEqual("tenant-1/workflow-1");
    [Fact] void should_read_the_scope_of_a_run_key() => _run.Scope.ShouldEqual(new WorkflowScope("tenant-1"));
    [Fact] void should_read_the_run_of_a_run_key() => _run.Run.ShouldEqual(new WorkflowRunId("run-1"));
    [Fact] void should_read_the_workflow_of_a_workflow_key() => _workflow.Workflow.ShouldEqual(new WorkflowId("workflow-1"));
    [Fact] void should_split_at_the_last_separator() => _scopeWithSeparator.Scope.ShouldEqual(new WorkflowScope("store/tenant-1"));
    [Fact] void should_refuse_a_key_without_a_separator() => Catch.Exception(() => WorkflowRunKey.Parse("nothing")).ShouldBeOfExactType<MalformedWorkflowGrainKey>();
}
