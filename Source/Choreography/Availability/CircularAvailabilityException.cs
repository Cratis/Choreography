// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Availability;

/// <summary>
/// Thrown when availability blocks form a cycle. Forwards to <see cref="Cratis.Arc.Availability.CircularAvailabilityException"/>.
/// </summary>
/// <param name="chain">The names that form the cycle.</param>
[Obsolete("The availability engine moved to Cratis.Arc.Availability (the Cratis.Arc.Availability package); use CircularAvailabilityException there. This forwarder is removed in the next major release.")]
public class CircularAvailabilityException(IReadOnlyList<string> chain) : Cratis.Arc.Availability.CircularAvailabilityException(chain);
