// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Availability;

/// <summary>
/// An availability manifest held in memory. Forwards to <see cref="Cratis.Arc.Availability.InMemoryAvailabilityManifest"/>.
/// </summary>
[Obsolete("The availability engine moved to Cratis.Arc.Availability (the Cratis.Arc.Availability package); use InMemoryAvailabilityManifest there. This forwarder is removed in the next major release.")]
public class InMemoryAvailabilityManifest : Cratis.Arc.Availability.InMemoryAvailabilityManifest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryAvailabilityManifest"/> class that declares nothing.
    /// </summary>
    public InMemoryAvailabilityManifest()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryAvailabilityManifest"/> class.
    /// </summary>
    /// <param name="current">The manifest.</param>
    public InMemoryAvailabilityManifest(Cratis.Arc.Availability.AvailabilityManifest current)
        : base(current)
    {
    }
}
