// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Contracts;

/// <summary>
/// The identity of a data contract - the shape of a payload a trigger, a signal or a node carries.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record WorkflowContractId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="WorkflowContractId"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator WorkflowContractId(string value) => new(value);
}
