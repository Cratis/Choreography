// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows;

/// <summary>
/// Who caused something in a workflow - a login, or the system itself.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record WorkflowActor(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The actor for work the system does on its own behalf.
    /// </summary>
    public static readonly WorkflowActor System = new("system");

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="WorkflowActor"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator WorkflowActor(string value) => new(value);
}
