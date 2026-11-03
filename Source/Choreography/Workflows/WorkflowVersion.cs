// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows;

/// <summary>
/// The version of a workflow definition.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record WorkflowVersion(int Value) : ConceptAs<int>(Value)
{
    /// <summary>
    /// No version - a workflow that has never been activated.
    /// </summary>
    public static readonly WorkflowVersion None = new(0);

    /// <summary>
    /// Implicitly convert from <see cref="int"/> to <see cref="WorkflowVersion"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator WorkflowVersion(int value) => new(value);
}
