// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Runs;

/// <summary>
/// The branch of a workflow run an execution belongs to - the root, or one item of a for-each fan-out.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record WorkflowBranchKey(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The root branch - everything outside a for-each.
    /// </summary>
    public static readonly WorkflowBranchKey Root = new(string.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="WorkflowBranchKey"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator WorkflowBranchKey(string value) => new(value);
}
