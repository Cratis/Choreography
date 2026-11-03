// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage.Sql.Patching;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cratis.Choreography.Storage.Sql.given;

/// <summary>
/// A real SQLite database on disk, backing a <see cref="SqlPatchStorage"/>.
/// </summary>
/// <remarks>
/// Real rather than substituted for the same reason the jobs specs are: a provider that ships a context it cannot create
/// tables for is invisible to a mocked <c lang="csharp">DbContext</c>. A file rather than <c lang="text">:memory:</c>, because
/// the storage opens a fresh context per operation and an in-memory database disappears with its last connection.
/// </remarks>
public class a_sqlite_patches_database : Specification, IDisposable
{
    string _databaseDirectory;
    protected DbContextOptions<PatchesDbContext> _contextOptions;
    protected SqlPatchStorage _storage;

    void Establish()
    {
        _databaseDirectory = Path.Combine(Path.GetTempPath(), $"cratis-choreography-patches-specs-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_databaseDirectory);
        _contextOptions = new DbContextOptionsBuilder<PatchesDbContext>()
            .UseSqlite($"Data Source={Path.Combine(_databaseDirectory, "patches.db")}")
            .Options;
        _storage = CreateStorage();
    }

    protected SqlPatchStorage CreateStorage() => new(Options.Create(new SqlPatchStorageOptions { OptionsResolver = () => _contextOptions }));

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        SqliteConnection.ClearAllPools();

        if (_databaseDirectory is not null && Directory.Exists(_databaseDirectory))
        {
            Directory.Delete(_databaseDirectory, recursive: true);
        }
    }
}
