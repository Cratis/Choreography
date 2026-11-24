// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Availability.Audiences;

/// <summary>
/// An audience source held in memory. Forwards to <see cref="Cratis.Arc.Availability.Audiences.InMemoryAudienceSource"/>.
/// </summary>
[Obsolete("The availability engine moved to Cratis.Arc.Availability (the Cratis.Arc.Availability package); use InMemoryAudienceSource there. This forwarder is removed in the next major release.")]
public class InMemoryAudienceSource : Cratis.Arc.Availability.Audiences.InMemoryAudienceSource;
