// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Contracts;

/// <summary>
/// A reference to evidence supporting something a workflow recorded - stored elsewhere, referred to here.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record EvidenceReference(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="EvidenceReference"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator EvidenceReference(string value) => new(value);
}
