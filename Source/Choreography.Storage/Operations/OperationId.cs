// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations;

/// <summary>
/// Represents the stable identity of a logical operation.
/// </summary>
/// <param name="Value">The actual value.</param>
public record OperationId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="OperationId"/>.
    /// </summary>
    /// <param name="value">The <see cref="Guid"/> to convert from.</param>
    public static implicit operator OperationId(Guid value) => new(value);

    /// <summary>
    /// Create a new unique <see cref="OperationId"/>.
    /// </summary>
    /// <returns>The new <see cref="OperationId"/>.</returns>
    public static OperationId New() => new(Guid.NewGuid());
}
