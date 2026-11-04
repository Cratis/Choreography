// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Storage.MongoDB;

/// <summary>
/// Represents the options for <see cref="MongoDBPatchStorage"/>.
/// </summary>
public class MongoDBPatchStorageOptions
{
    /// <summary>
    /// Gets or sets the name of the database holding the patch records. Defaults to "choreography".
    /// </summary>
    /// <remarks>
    /// Patches describe the store itself, so the records live in one database for the whole store - not one per scope and namespace
    /// the way jobs do.
    /// </remarks>
    public string DatabaseName { get; set; } = "choreography";
}
