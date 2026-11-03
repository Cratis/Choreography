// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Waits;

/// <summary>
/// When a wait runs out of time.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record WaitDeadline(DateTimeOffset Value) : ConceptAs<DateTimeOffset>(Value)
{
    /// <summary>
    /// No deadline - the wait stays open until it is resolved or cancelled.
    /// </summary>
    public static readonly WaitDeadline None = new(DateTimeOffset.MaxValue);

    /// <summary>
    /// Implicitly convert from <see cref="DateTimeOffset"/> to <see cref="WaitDeadline"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator WaitDeadline(DateTimeOffset value) => new(value);

    /// <summary>
    /// Gets whether the deadline has passed at a point in time.
    /// </summary>
    /// <param name="now">The point in time.</param>
    /// <returns>True if the wait is due.</returns>
    public bool IsDueAt(DateTimeOffset now) => this != None && Value <= now;
}
