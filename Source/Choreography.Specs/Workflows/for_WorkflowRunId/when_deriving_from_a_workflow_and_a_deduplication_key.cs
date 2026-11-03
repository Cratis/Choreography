// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.for_WorkflowRunId;

/// <summary>
/// A run's identity is where its facts are kept, so the same trigger must always find the same run - also across releases.
/// </summary>
public class when_deriving_from_a_workflow_and_a_deduplication_key : Specification
{
    WorkflowRunId _first = null!;
    WorkflowRunId _again = null!;
    WorkflowRunId _other = null!;

    void Because()
    {
        _first = WorkflowRunId.For("workflow-1", "dedup-1");
        _again = WorkflowRunId.For("workflow-1", "dedup-1");
        _other = WorkflowRunId.For("workflow-1", "dedup-2");
    }

    [Fact] void should_derive_the_same_run_for_the_same_trigger() => _again.ShouldEqual(_first);
    [Fact] void should_derive_another_run_for_another_trigger() => _other.ShouldNotEqual(_first);
    [Fact] void should_derive_the_identity_every_release_has_derived() => _first.Value.ShouldEqual("ffeb9d79-498d-53e1-a405-376109c9ec3b");
}
