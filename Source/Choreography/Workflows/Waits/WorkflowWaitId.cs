// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Waits;

/// <summary>
/// The identity of a wait a workflow run opened.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record WorkflowWaitId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="WorkflowWaitId"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator WorkflowWaitId(string value) => new(value);
}
