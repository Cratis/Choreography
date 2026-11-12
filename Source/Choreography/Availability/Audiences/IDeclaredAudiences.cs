// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Availability.Audiences;

/// <summary>
/// Defines the audiences the model declares.
/// </summary>
/// <remarks>
/// A grant only matches a gate when its audience is declared. A grant for an undeclared audience is kept and reported.
/// </remarks>
public interface IDeclaredAudiences
{
    /// <summary>
    /// Gets every declared audience.
    /// </summary>
    IReadOnlyCollection<AudienceName> All { get; }

    /// <summary>
    /// Determines whether an audience is declared.
    /// </summary>
    /// <param name="audience">The audience to check.</param>
    /// <returns><see langword="true"/> if declared; otherwise <see langword="false"/>.</returns>
    bool IsDeclared(AudienceName audience);
}
