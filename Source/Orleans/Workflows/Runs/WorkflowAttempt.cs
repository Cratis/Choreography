// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Runs;

/// <summary>
/// Which attempt at a node execution something belongs to, counted from one.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record WorkflowAttempt(int Value) : ConceptAs<int>(Value)
{
    /// <summary>
    /// The first attempt.
    /// </summary>
    public static readonly WorkflowAttempt First = new(1);

    /// <summary>
    /// Gets the attempt after this one.
    /// </summary>
    public WorkflowAttempt Next => new(Value + 1);

    /// <summary>
    /// Implicitly convert from <see cref="int"/> to <see cref="WorkflowAttempt"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator WorkflowAttempt(int value) => new(value);
}
