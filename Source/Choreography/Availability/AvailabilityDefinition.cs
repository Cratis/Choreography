// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Availability;

/// <summary>
/// A reusable 'availability' block.
/// </summary>
/// <param name="Name">The name of the block.</param>
/// <param name="Expression">What must hold. The 'for' and 'when' lines of a block are combined with 'and'.</param>
public record AvailabilityDefinition(string Name, AvailabilityExpression Expression)
{
    /// <summary>
    /// Creates a block from its lines, which must all hold.
    /// </summary>
    /// <param name="name">The name of the block.</param>
    /// <param name="lines">The lines, in authored order.</param>
    /// <returns>The definition.</returns>
    public static AvailabilityDefinition FromLines(string name, params AvailabilityExpression[] lines) =>
        new(name, lines.Length == 1 ? lines[0] : new AvailabilityAnd(lines));
}
