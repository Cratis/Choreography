// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Definitions;

namespace Cratis.Choreography.Workflows.Runs;

/// <summary>
/// The answer to a request to run a workflow.
/// </summary>
/// <param name="Admitted">Whether a run exists for the trigger - newly or from an earlier delivery of it.</param>
/// <param name="Run">The <see cref="WorkflowRunId"/> the trigger maps to.</param>
/// <param name="Rejections">Why it was not admitted - empty when it was.</param>
public record WorkflowAdmission(bool Admitted, WorkflowRunId Run, IEnumerable<WorkflowAdmissionRejection> Rejections)
{
    /// <summary>
    /// Creates an admission.
    /// </summary>
    /// <param name="run">The <see cref="WorkflowRunId"/>.</param>
    /// <returns>The <see cref="WorkflowAdmission"/>.</returns>
    public static WorkflowAdmission To(WorkflowRunId run) => new(true, run, []);

    /// <summary>
    /// Creates a rejection.
    /// </summary>
    /// <param name="run">The <see cref="WorkflowRunId"/> the trigger maps to.</param>
    /// <param name="rejections">Why it was not admitted.</param>
    /// <returns>The <see cref="WorkflowAdmission"/>.</returns>
    public static WorkflowAdmission Rejected(WorkflowRunId run, IEnumerable<WorkflowAdmissionRejection> rejections) => new(false, run, [.. rejections]);
}
