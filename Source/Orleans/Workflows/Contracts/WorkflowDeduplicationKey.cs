// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Contracts;

/// <summary>
/// The key that makes a trigger idempotent - the same workflow admitted twice with the same key is the same run.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record WorkflowDeduplicationKey(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="WorkflowDeduplicationKey"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator WorkflowDeduplicationKey(string value) => new(value);
}
