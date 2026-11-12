// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Availability.Audiences;

namespace Cratis.Choreography.Availability;

/// <summary>
/// A gate expression: 'for', 'when' and 'with' parts combined with 'and', 'or' and parentheses.
/// </summary>
/// <remarks>
/// The tree keeps authored order, and parentheses are the tree's shape.
/// </remarks>
public abstract record AvailabilityExpression
{
    /// <summary>
    /// Creates 'for' over one or more audiences, which holds when any is held.
    /// </summary>
    /// <param name="audiences">The audiences.</param>
    /// <returns>The expression.</returns>
    public static AvailabilityExpression For(params AudienceName[] audiences) =>
        audiences.Length == 1 ? new AvailableForAudience(audiences[0]) : new AvailabilityOr([.. audiences.Select(_ => new AvailableForAudience(_))]);

    /// <summary>
    /// Creates 'when' over a condition.
    /// </summary>
    /// <param name="condition">The condition.</param>
    /// <returns>The expression.</returns>
    public static AvailabilityExpression When(IAvailabilityCondition condition) => new AvailableWhen(condition);

    /// <summary>
    /// Creates 'with' over a named availability block.
    /// </summary>
    /// <param name="name">The name of the block.</param>
    /// <returns>The expression.</returns>
    public static AvailabilityExpression With(string name) => new AvailableWith(name);

    /// <summary>
    /// Combines expressions with 'and', in the order given.
    /// </summary>
    /// <param name="parts">The parts.</param>
    /// <returns>The expression.</returns>
    public static AvailabilityExpression AllOf(params AvailabilityExpression[] parts) => new AvailabilityAnd(parts);

    /// <summary>
    /// Combines expressions with 'or', in the order given.
    /// </summary>
    /// <param name="parts">The parts.</param>
    /// <returns>The expression.</returns>
    public static AvailabilityExpression AnyOf(params AvailabilityExpression[] parts) => new AvailabilityOr(parts);
}
