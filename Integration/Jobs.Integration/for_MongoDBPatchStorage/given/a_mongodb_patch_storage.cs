// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage.MongoDB;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Cratis.Choreography.Jobs.Integration.for_MongoDBPatchStorage.given;

/// <summary>
/// A <see cref="MongoDBPatchStorage"/> against a real MongoDB on <c lang="text">localhost:27017</c>, in a database of its own
/// that is dropped when the spec is done.
/// </summary>
public class a_mongodb_patch_storage : Specification, IDisposable
{
    readonly string _databaseName = $"choreography-patches-{Guid.NewGuid():N}";
    readonly MongoClient _client = new(JobsClusterFixture.ConnectionString);
    protected MongoDBPatchStorage _storage;

    void Establish() => _storage = CreateStorage();

    protected MongoDBPatchStorage CreateStorage() =>
        new(_client, Options.Create(new MongoDBPatchStorageOptions { DatabaseName = _databaseName }));

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _client.DropDatabase(_databaseName);
        _client.Dispose();
    }
}
