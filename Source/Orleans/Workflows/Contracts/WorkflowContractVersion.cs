// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Contracts;

/// <summary>
/// The version of a data contract.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record WorkflowContractVersion(int Value) : ConceptAs<int>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="int"/> to <see cref="WorkflowContractVersion"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator WorkflowContractVersion(int value) => new(value);
}
