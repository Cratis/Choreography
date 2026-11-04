// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching;

/// <summary>
/// Carries the caller's silo as a hint for where the patch manager should relocate to.
/// </summary>
internal static class PatchRelocation
{
    /// <summary>
    /// The request-context key for the caller's silo address.
    /// </summary>
    internal const string CallerSiloKey = "Cratis.Choreography.Patching.CallerSilo";

    /// <summary>
    /// Reads the caller's silo, only when it is a typed Orleans address.
    /// </summary>
    /// <returns>The supplied address, or null when the caller sent none or sent something that is not an address.</returns>
    internal static SiloAddress? CallerSilo() => RequestContext.Get(CallerSiloKey) as SiloAddress;
}
