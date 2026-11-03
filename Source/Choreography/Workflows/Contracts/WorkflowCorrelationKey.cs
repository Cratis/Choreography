// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Contracts;

/// <summary>
/// The key that correlates a workflow run with the outside world - what a signal is matched on.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record WorkflowCorrelationKey(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The key for a run nothing outside correlates with.
    /// </summary>
    public static readonly WorkflowCorrelationKey None = new(string.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="WorkflowCorrelationKey"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator WorkflowCorrelationKey(string value) => new(value);
}
