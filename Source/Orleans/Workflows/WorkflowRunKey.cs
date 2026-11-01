// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows;

/// <summary>
/// The key of a workflow run grain - the scope the run belongs to and the run itself.
/// </summary>
/// <param name="Scope">The <see cref="WorkflowScope"/> the run belongs to.</param>
/// <param name="Run">The <see cref="WorkflowRunId"/>.</param>
/// <remarks>
/// The scope is part of the key, never part of a payload: a run grain decides whose event store it
/// reads from what it was activated as, so nothing a caller sends can move it to another scope.
/// </remarks>
public record WorkflowRunKey(WorkflowScope Scope, WorkflowRunId Run)
{
    /// <summary>
    /// Parses a key from its grain key text.
    /// </summary>
    /// <param name="key">The text, as produced by <see cref="ToString"/>.</param>
    /// <returns>The <see cref="WorkflowRunKey"/>.</returns>
    public static WorkflowRunKey Parse(string key)
    {
        var (scope, run) = GrainKeyText.Split(key);
        return new(new WorkflowScope(scope), new WorkflowRunId(run));
    }

    /// <inheritdoc/>
    public override string ToString() => GrainKeyText.Join(Scope.Value, Run.Value);
}
