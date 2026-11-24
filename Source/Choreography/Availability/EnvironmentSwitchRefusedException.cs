// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Availability;

/// <summary>
/// Thrown when granting every declared audience is refused. Forwards to <see cref="Cratis.Arc.Availability.EnvironmentSwitchRefusedException"/>.
/// </summary>
/// <param name="environmentName">The environment the switch was refused in.</param>
[Obsolete("The availability engine moved to Cratis.Arc.Availability (the Cratis.Arc.Availability package); use EnvironmentSwitchRefusedException there. This forwarder is removed in the next major release.")]
public class EnvironmentSwitchRefusedException(string? environmentName) : Cratis.Arc.Availability.EnvironmentSwitchRefusedException(environmentName);
