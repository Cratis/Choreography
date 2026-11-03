// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson.Serialization.Attributes;

namespace Cratis.Choreography.Storage.MongoDB.Patching;

/// <summary>
/// Represents a document holding one of the versions patching keeps.
/// </summary>
internal class PatchVersionDocument
{
    /// <summary>
    /// The identifier of the document holding the version of the newest patch that has been applied.
    /// </summary>
    internal const string Patched = "patched";

    /// <summary>
    /// The identifier of the document holding the version of the newest host that has patched the store.
    /// </summary>
    internal const string Host = "host";

    /// <summary>
    /// Gets or sets the identifier of the version.
    /// </summary>
    [BsonId]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version.
    /// </summary>
    [BsonElement("version")]
    public string Version { get; set; } = string.Empty;
}
