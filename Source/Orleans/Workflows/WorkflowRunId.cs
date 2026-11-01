// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Contracts;

namespace Cratis.Orleans.Workflows;

/// <summary>
/// The identity of one run of a workflow - the key that run's whole history is kept under.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record WorkflowRunId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="WorkflowRunId"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator WorkflowRunId(string value) => new(value);

    /// <summary>
    /// Derives the run a trigger admits - the same workflow and deduplication key are always the same
    /// run, so a trigger delivered twice finds the run the first delivery started.
    /// </summary>
    /// <param name="workflow">The <see cref="WorkflowId"/> the run belongs to.</param>
    /// <param name="deduplication">The trigger's <see cref="WorkflowDeduplicationKey"/>.</param>
    /// <returns>The <see cref="WorkflowRunId"/>.</returns>
    public static WorkflowRunId For(WorkflowId workflow, WorkflowDeduplicationKey deduplication) =>
        new(DeterministicId.From("workflow-run", workflow.Value, deduplication.Value).ToString());
}
