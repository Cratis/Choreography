// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Availability.Audiences;

/// <summary>
/// The audiences a model declares. Forwards to <see cref="Cratis.Arc.Availability.Audiences.DeclaredAudiences"/>.
/// </summary>
[Obsolete("The availability engine moved to Cratis.Arc.Availability (the Cratis.Arc.Availability package); use DeclaredAudiences there. This forwarder is removed in the next major release.")]
public class DeclaredAudiences : Cratis.Arc.Availability.Audiences.DeclaredAudiences
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeclaredAudiences"/> class.
    /// </summary>
    /// <param name="audiences">The declared audiences.</param>
    public DeclaredAudiences(IEnumerable<Cratis.Arc.Availability.Audiences.AudienceName> audiences)
        : base(audiences)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeclaredAudiences"/> class.
    /// </summary>
    /// <param name="audiences">The declared audiences.</param>
    public DeclaredAudiences(params Cratis.Arc.Availability.Audiences.AudienceName[] audiences)
        : base(audiences)
    {
    }
}
