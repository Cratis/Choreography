// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Availability.Audiences;

/// <summary>
/// An in-memory <see cref="IDeclaredAudiences"/>.
/// </summary>
/// <param name="audiences">The declared audiences.</param>
public class DeclaredAudiences(IEnumerable<AudienceName> audiences) : IDeclaredAudiences
{
    readonly HashSet<AudienceName> _audiences = [.. audiences];

    /// <summary>
    /// Initializes a new instance of the <see cref="DeclaredAudiences"/> class.
    /// </summary>
    /// <param name="audiences">The declared audiences.</param>
    public DeclaredAudiences(params AudienceName[] audiences)
        : this((IEnumerable<AudienceName>)audiences)
    {
    }

    /// <inheritdoc/>
    public IReadOnlyCollection<AudienceName> All => _audiences;

    /// <inheritdoc/>
    public bool IsDeclared(AudienceName audience) => _audiences.Contains(audience);
}
