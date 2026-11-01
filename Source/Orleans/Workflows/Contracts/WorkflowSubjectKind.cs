// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Contracts;

/// <summary>
/// The kind of thing a workflow run is about - an opaque key the engine never interprets.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record WorkflowSubjectKind(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="WorkflowSubjectKind"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator WorkflowSubjectKind(string value) => new(value);
}
