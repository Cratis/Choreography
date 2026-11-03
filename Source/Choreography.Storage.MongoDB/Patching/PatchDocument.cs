// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson.Serialization.Attributes;

namespace Cratis.Choreography.Storage.MongoDB.Patching;

/// <summary>
/// Represents the document recording an applied patch.
/// </summary>
internal class PatchDocument
{
    /// <summary>
    /// Gets or sets the name of the patch, which identifies the document.
    /// </summary>
    [BsonId]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version the patch belongs to.
    /// </summary>
    [BsonElement("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the patch was applied, in UTC.
    /// </summary>
    [BsonElement("appliedAt")]
    public DateTime AppliedAt { get; set; }
}
