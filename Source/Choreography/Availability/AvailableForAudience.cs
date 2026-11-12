// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Availability.Audiences;

namespace Cratis.Choreography.Availability;

/// <summary>
/// 'for': holds when the request holds the audience.
/// </summary>
/// <param name="Audience">The audience.</param>
public record AvailableForAudience(AudienceName Audience) : AvailabilityExpression;
