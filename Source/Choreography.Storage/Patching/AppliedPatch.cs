// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching;

/// <summary>
/// Represents the record of a patch that has been applied.
/// </summary>
/// <param name="Name">The name of the patch.</param>
/// <param name="Version">The version the patch belongs to.</param>
/// <param name="AppliedAt">When the patch was applied.</param>
public record AppliedPatch(string Name, SemanticVersion Version, DateTimeOffset AppliedAt);
