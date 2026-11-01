// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows;
using Cratis.Orleans.Workflows.Contracts;
using Cratis.Orleans.Workflows.Runs;

namespace Cratis.Orleans.Chronicle.Workflows.Listing;

/// <summary>
/// Lists a run's identity, status and activity. The read model can lag; the run grain remains authoritative.
/// </summary>
/// <param name="Id">The run.</param>
/// <param name="Workflow">The workflow.</param>
/// <param name="Area">The key of whoever owns the workflow.</param>
/// <param name="Version">The pinned version.</param>
/// <param name="Subject">The subject.</param>
/// <param name="Correlation">The correlation.</param>
/// <param name="Status">The run status.</param>
/// <param name="AdmittedAt">Admission time.</param>
/// <param name="LastActivityAt">Latest activity time.</param>
/// <param name="CompletedAt">Completion time, if terminal.</param>
public record WorkflowRun(
    WorkflowRunId Id,
    WorkflowId Workflow,
    WorkflowAreaKey Area,
    WorkflowVersion Version,
    WorkflowSubjectReference Subject,
    WorkflowCorrelationKey Correlation,
    WorkflowRunStatus Status,
    DateTimeOffset AdmittedAt,
    DateTimeOffset LastActivityAt,
    DateTimeOffset? CompletedAt)
{
    /// <summary>
    /// Gets execution activity used to derive concurrent run status.
    /// </summary>
    public IEnumerable<WorkflowRunExecution> Executions { get; init; } = [];

    /// <summary>
    /// Gets wait identities used to close the correct execution.
    /// </summary>
    public IEnumerable<WorkflowRunWait> Waits { get; init; } = [];

    /// <summary>
    /// Gets the wave jobs a stop is still owed to after cancellation.
    /// </summary>
    public IEnumerable<WorkflowJobReference> PendingJobStops { get; init; } = [];
}
