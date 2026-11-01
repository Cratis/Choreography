// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows;

/// <summary>
/// The identity of a logical workflow - the key its definition is looked up by.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record WorkflowId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="WorkflowId"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator WorkflowId(string value) => new(value);

    /// <summary>
    /// Creates a new unique workflow identity.
    /// </summary>
    /// <returns>A new <see cref="WorkflowId"/>.</returns>
    public static WorkflowId New() => new(Guid.NewGuid().ToString());
}
