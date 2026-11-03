// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching;

/// <summary>
/// Exception that gets thrown when patching has not completed within the time a host is willing to wait for it.
/// </summary>
/// <param name="hostVersion">The version of the host that waited.</param>
/// <param name="waited">How long it waited.</param>
/// <param name="lastFailure">The last failure the host saw while it waited.</param>
public class PatchingDidNotComplete(SemanticVersion hostVersion, TimeSpan waited, Exception? lastFailure)
    : Exception($"Patching did not complete for host {hostVersion} within {waited}. A patch may still be running, or the silo holding the patch manager is unreachable.", lastFailure);
