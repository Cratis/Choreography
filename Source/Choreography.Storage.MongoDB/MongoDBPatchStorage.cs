// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Patching;
using Cratis.Choreography.Storage.MongoDB.Patching;
using Cratis.Choreography.Storage.Patching;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Cratis.Choreography.Storage.MongoDB;

/// <summary>
/// Represents an <see cref="IPatchStorage"/> implementation for MongoDB.
/// </summary>
/// <param name="client">The <see cref="IMongoClient"/> to resolve the database from.</param>
/// <param name="options"><see cref="MongoDBPatchStorageOptions"/> to use.</param>
public class MongoDBPatchStorage(IMongoClient client, IOptions<MongoDBPatchStorageOptions> options) : IPatchStorage
{
    IMongoDatabase Database => client.GetDatabase(options.Value.DatabaseName);

    IMongoCollection<PatchDocument> Patches => Database.GetCollection<PatchDocument>(WellKnownCollectionNames.Patches);

    IMongoCollection<PatchVersionDocument> Versions => Database.GetCollection<PatchVersionDocument>(WellKnownCollectionNames.PatchVersions);

    /// <inheritdoc/>
    public async Task<IEnumerable<AppliedPatch>> GetAll()
    {
        var documents = await Patches.Find(FilterDefinition<PatchDocument>.Empty).ToListAsync();
        return documents.Select(_ => new AppliedPatch(_.Name, SemanticVersion.Parse(_.Version), new DateTimeOffset(DateTime.SpecifyKind(_.AppliedAt, DateTimeKind.Utc))));
    }

    /// <inheritdoc/>
    public async Task<bool> Has(string patchName) =>
        await Patches.CountDocumentsAsync(_ => _.Name == patchName, new CountOptions { Limit = 1 }) > 0;

    /// <inheritdoc/>
    public async Task Save(AppliedPatch patch) =>
        await Patches.ReplaceOneAsync(
            _ => _.Name == patch.Name,
            new PatchDocument { Name = patch.Name, Version = patch.Version.ToString(), AppliedAt = patch.AppliedAt.UtcDateTime },
            new ReplaceOptions { IsUpsert = true });

    /// <inheritdoc/>
    public Task<SemanticVersion?> GetPatchedVersion() => GetVersion(PatchVersionDocument.Patched);

    /// <inheritdoc/>
    public Task SetPatchedVersion(SemanticVersion version) => SetVersion(PatchVersionDocument.Patched, version);

    /// <inheritdoc/>
    public Task<SemanticVersion?> GetHostVersion() => GetVersion(PatchVersionDocument.Host);

    /// <inheritdoc/>
    public Task SetHostVersion(SemanticVersion version) => SetVersion(PatchVersionDocument.Host, version);

    async Task<SemanticVersion?> GetVersion(string id)
    {
        var document = await Versions.Find(_ => _.Id == id).FirstOrDefaultAsync();
        return document is null ? null : SemanticVersion.Parse(document.Version);
    }

    async Task SetVersion(string id, SemanticVersion version) =>
        await Versions.ReplaceOneAsync(
            _ => _.Id == id,
            new PatchVersionDocument { Id = id, Version = version.ToString() },
            new ReplaceOptions { IsUpsert = true });
}
