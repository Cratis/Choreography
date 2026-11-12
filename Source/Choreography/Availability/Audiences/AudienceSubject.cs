// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Availability.Audiences;

/// <summary>
/// Identifies who a request is for, so the audiences that apply to it can be resolved.
/// </summary>
/// <param name="Tenant">The tenant, or <see langword="null"/> when the request names none.</param>
/// <param name="User">The user, or <see langword="null"/> when the request has no user.</param>
/// <param name="IsSystem">Whether the caller is the system itself, which holds system-level audiences.</param>
public record AudienceSubject(string? Tenant, string? User, bool IsSystem = false);
