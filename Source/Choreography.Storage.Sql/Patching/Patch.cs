// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Choreography.Storage.Sql.Patching;

/// <summary>
/// Represents the entity recording an applied patch.
/// </summary>
public class Patch
{
    /// <summary>
    /// Gets or sets the name of the patch, which identifies the row.
    /// </summary>
    [Key]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version the patch belongs to.
    /// </summary>
    [MaxLength(256)]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the patch was applied.
    /// </summary>
    public DateTimeOffset AppliedAt { get; set; }
}
