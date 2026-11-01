// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Contracts;

/// <summary>
/// The key of whatever caused a workflow trigger - kept for tracing, never interpreted.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record WorkflowCausationKey(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The key for a trigger with no recorded cause.
    /// </summary>
    public static readonly WorkflowCausationKey None = new(string.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="WorkflowCausationKey"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator WorkflowCausationKey(string value) => new(value);
}
