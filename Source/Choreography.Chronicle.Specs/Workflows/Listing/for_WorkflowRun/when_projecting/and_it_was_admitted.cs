// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Runs;

namespace Cratis.Choreography.Chronicle.Workflows.Listing.for_WorkflowRun.when_projecting;

public class and_it_was_admitted : Specification
{
    readonly WorkflowRunId _run = Guid.NewGuid().ToString();
    readonly WorkflowId _workflow = WorkflowId.New();
    ReadModelScenario<WorkflowRun> _scenario = null!;

    void Establish() => _scenario = new();

    async Task Because() => await _scenario.Given.ForEventSource(_run).Events(given.an_admission(_workflow));

    [Fact] void should_name_the_workflow() => _scenario.Instance.Workflow.ShouldEqual(_workflow);
    [Fact] void should_name_the_area() => _scenario.Instance.Area.ShouldEqual(new WorkflowAreaKey("product"));
    [Fact] void should_name_the_version() => _scenario.Instance.Version.ShouldEqual(new WorkflowVersion(3));
    [Fact] void should_carry_the_subject_from_the_trigger() => _scenario.Instance.Subject.ShouldEqual(new WorkflowSubjectReference("issue", "issue-42"));
    [Fact] void should_carry_the_correlation_from_the_trigger() => _scenario.Instance.Correlation.ShouldEqual(new WorkflowCorrelationKey("correlation-42"));
    [Fact] void should_be_running() => _scenario.Instance.Status.ShouldEqual(WorkflowRunStatus.Running);
    [Fact] void should_not_be_completed() => _scenario.Instance.CompletedAt.ShouldBeNull();
}
