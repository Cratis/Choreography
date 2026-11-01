// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows;

/// <summary>
/// An opaque key naming who owns a workflow. The engine never interprets it - it is carried, stored and compared only by the code that defines areas.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record WorkflowAreaKey(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="WorkflowAreaKey"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator WorkflowAreaKey(string value) => new(value);
}
