// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Waits;

/// <summary>
/// The key a suspended node execution is completed by - for an extension invocation, its invocation identity.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record WorkflowCompletionKey(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The key of nothing - a suspension must name a key.
    /// </summary>
    public static readonly WorkflowCompletionKey None = new(string.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="WorkflowCompletionKey"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator WorkflowCompletionKey(string value) => new(value);
}
