// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Availability;

/// <summary>
/// The catalog of named availability definitions. Forwards to <see cref="Cratis.Arc.Availability.AvailabilityCatalog"/>.
/// </summary>
[Obsolete("The availability engine moved to Cratis.Arc.Availability (the Cratis.Arc.Availability package); use AvailabilityCatalog there. This forwarder is removed in the next major release.")]
public class AvailabilityCatalog : Cratis.Arc.Availability.AvailabilityCatalog;
