// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage.Sql;
using Cratis.Choreography.Storage.Sql.Patching;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Cratis.Choreography.Jobs.Integration.for_SqlPatchStorage.given;

/// <summary>
/// A real database of the provider a derived context names, created empty and dropped when the spec is done.
/// </summary>
/// <remarks>
/// The storage migrates on first use, so the database starts without a schema: the migration itself is what these specs prove,
/// on the engines the package supports rather than only on SQLite.
/// </remarks>
public abstract class a_patch_database : Specification, IDisposable
{
    DbContextOptions<PatchesDbContext> _options;
    protected SqlPatchStorage _storage;

    protected abstract DbContextOptionsBuilder<PatchesDbContext> Configure(DbContextOptionsBuilder<PatchesDbContext> builder, string databaseName);

    void Establish()
    {
        _options = Configure(new(), $"cratis_choreography_patch_specs_{Guid.NewGuid():N}").Options;
        _storage = CreateStorage();
    }

    protected SqlPatchStorage CreateStorage() => new(Options.Create(new SqlPatchStorageOptions { OptionsResolver = () => _options }));

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        using var context = new PatchesDbContext(_options);
        if (context.Database.IsNpgsql())
        {
            NpgsqlConnection.ClearAllPools();
        }

        if (context.Database.IsSqlServer())
        {
            SqlConnection.ClearAllPools();
        }

        context.Database.EnsureDeleted();
    }
}
