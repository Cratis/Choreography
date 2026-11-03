// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows;

/// <summary>
/// The key of a logical workflow grain - the scope it belongs to and the workflow itself.
/// </summary>
/// <param name="Scope">The <see cref="WorkflowScope"/> the workflow belongs to.</param>
/// <param name="Workflow">The <see cref="WorkflowId"/>.</param>
public record WorkflowKey(WorkflowScope Scope, WorkflowId Workflow)
{
    /// <summary>
    /// Parses a key from its grain key text.
    /// </summary>
    /// <param name="key">The text, as produced by <see cref="ToString"/>.</param>
    /// <returns>The <see cref="WorkflowKey"/>.</returns>
    public static WorkflowKey Parse(string key)
    {
        var (scope, workflow) = GrainKeyText.Split(key);
        return new(new WorkflowScope(scope), new WorkflowId(workflow));
    }

    /// <inheritdoc/>
    public override string ToString() => GrainKeyText.Join(Scope.Value, Workflow.Value);
}
