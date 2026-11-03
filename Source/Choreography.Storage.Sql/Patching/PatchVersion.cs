// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Choreography.Storage.Sql.Patching;

/// <summary>
/// Represents the entity holding one of the versions patching keeps.
/// </summary>
public class PatchVersion
{
    /// <summary>
    /// The identifier of the row holding the version of the newest patch that has been applied.
    /// </summary>
    public const string Patched = "patched";

    /// <summary>
    /// The identifier of the row holding the version of the newest host that has patched the store.
    /// </summary>
    public const string Host = "host";

    /// <summary>
    /// Gets or sets the identifier of the version.
    /// </summary>
    [Key]
    [MaxLength(64)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version.
    /// </summary>
    [MaxLength(256)]
    public string Version { get; set; } = string.Empty;
}
