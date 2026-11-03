// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Runs;

/// <summary>
/// Why something in a workflow run failed, was skipped or was cancelled - for a person to read.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record WorkflowFailureReason(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// No reason given.
    /// </summary>
    public static readonly WorkflowFailureReason None = new(string.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="WorkflowFailureReason"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator WorkflowFailureReason(string value) => new(value);
}
