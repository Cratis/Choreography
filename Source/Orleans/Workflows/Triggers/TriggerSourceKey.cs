// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Triggers;

/// <summary>
/// Names where a trigger came from - a signal, a stage hook, a schedule or a person - opaque to the engine.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record TriggerSourceKey(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="TriggerSourceKey"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator TriggerSourceKey(string value) => new(value);
}
