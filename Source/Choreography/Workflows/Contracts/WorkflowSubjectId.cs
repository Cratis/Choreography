// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Contracts;

/// <summary>
/// The identity of the thing a workflow run is about, within its kind.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record WorkflowSubjectId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="WorkflowSubjectId"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator WorkflowSubjectId(string value) => new(value);
}
