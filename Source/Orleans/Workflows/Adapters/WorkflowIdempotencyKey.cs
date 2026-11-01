// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Adapters;

/// <summary>
/// The key a performer deduplicates external effects on. It is the same for every attempt of one node execution.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record WorkflowIdempotencyKey(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="WorkflowIdempotencyKey"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator WorkflowIdempotencyKey(string value) => new(value);
}
