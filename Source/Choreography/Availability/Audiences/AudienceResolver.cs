// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Availability.Audiences;

/// <summary>
/// Resolves the audiences that apply to a request. Forwards to <see cref="Cratis.Arc.Availability.Audiences.AudienceResolver"/>.
/// </summary>
/// <param name="source">Where grants come from.</param>
/// <param name="declared">The audiences the model declares.</param>
/// <param name="options">The availability options.</param>
/// <param name="observer">Optional observer told about undeclared grants.</param>
[Obsolete("The availability engine moved to Cratis.Arc.Availability (the Cratis.Arc.Availability package); use AudienceResolver there. This forwarder is removed in the next major release.")]
public class AudienceResolver(
    Cratis.Arc.Availability.Audiences.IAudienceSource source,
    Cratis.Arc.Availability.Audiences.IDeclaredAudiences declared,
    Cratis.Arc.Availability.AvailabilityOptions options,
    Cratis.Arc.Availability.Audiences.IUndeclaredAudienceObserver? observer = null)
    : Cratis.Arc.Availability.Audiences.AudienceResolver(source, declared, options, observer);
