// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Availability;

/// <summary>
/// Thrown when an availability name is not defined. Forwards to <see cref="Cratis.Arc.Availability.UndefinedAvailabilityException"/>.
/// </summary>
/// <param name="name">The undefined name.</param>
[Obsolete("The availability engine moved to Cratis.Arc.Availability (the Cratis.Arc.Availability package); use UndefinedAvailabilityException there. This forwarder is removed in the next major release.")]
public class UndefinedAvailabilityException(string name) : Cratis.Arc.Availability.UndefinedAvailabilityException(name);
