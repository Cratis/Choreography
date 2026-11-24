// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Availability;

/// <summary>
/// Options for the availability engine. Forwards to <see cref="Cratis.Arc.Availability.AvailabilityOptions"/>.
/// </summary>
[Obsolete("The availability engine moved to Cratis.Arc.Availability (the Cratis.Arc.Availability package); use AvailabilityOptions there. This forwarder is removed in the next major release.")]
public class AvailabilityOptions : Cratis.Arc.Availability.AvailabilityOptions;
