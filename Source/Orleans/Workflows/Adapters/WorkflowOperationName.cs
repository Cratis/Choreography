// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Adapters;

/// <summary>
/// The name of one operation of a staged node performer.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record WorkflowOperationName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// No operation - the whole node is performed at once.
    /// </summary>
    public static readonly WorkflowOperationName None = new(string.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="WorkflowOperationName"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator WorkflowOperationName(string value) => new(value);
}
