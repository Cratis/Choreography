// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Patching;
using Cratis.Choreography.Storage.Patching;
using Cratis.Choreography.Storage.Sql.Patching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cratis.Choreography.Storage.Sql;

/// <summary>
/// Represents an <see cref="IPatchStorage"/> implementation for SQL databases through EF Core.
/// </summary>
/// <param name="options"><see cref="SqlPatchStorageOptions"/> to use.</param>
/// <remarks>
/// The first use applies the migrations, so the tables exist before anything reads or writes them - the host only ever
/// supplies options, and never sees the context.
/// </remarks>
public class SqlPatchStorage(IOptions<SqlPatchStorageOptions> options) : IPatchStorage
{
    static readonly Lock _migrationLock = new();
    volatile bool _migrated;

    /// <inheritdoc/>
    public async Task<IEnumerable<AppliedPatch>> GetAll()
    {
        await using var context = CreateContext();
        var patches = await context.Patches.AsNoTracking().ToListAsync();
        return patches.Select(_ => new AppliedPatch(_.Name, SemanticVersion.Parse(_.Version), _.AppliedAt));
    }

    /// <inheritdoc/>
    public async Task<bool> Has(string patchName)
    {
        await using var context = CreateContext();
        return await context.Patches.AnyAsync(_ => _.Name == patchName);
    }

    /// <inheritdoc/>
    public async Task Save(AppliedPatch patch)
    {
        await using var context = CreateContext();
        var entity = await context.Patches.FindAsync(patch.Name);
        if (entity is null)
        {
            entity = new Patch { Name = patch.Name };
            context.Patches.Add(entity);
        }

        entity.Version = patch.Version.ToString();
        entity.AppliedAt = patch.AppliedAt.ToUniversalTime();
        await context.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public Task<SemanticVersion?> GetPatchedVersion() => GetVersion(PatchVersion.Patched);

    /// <inheritdoc/>
    public Task SetPatchedVersion(SemanticVersion version) => SetVersion(PatchVersion.Patched, version);

    /// <inheritdoc/>
    public Task<SemanticVersion?> GetHostVersion() => GetVersion(PatchVersion.Host);

    /// <inheritdoc/>
    public Task SetHostVersion(SemanticVersion version) => SetVersion(PatchVersion.Host, version);

    async Task<SemanticVersion?> GetVersion(string id)
    {
        await using var context = CreateContext();
        var entity = await context.PatchVersions.AsNoTracking().FirstOrDefaultAsync(_ => _.Id == id);
        return entity is null ? null : SemanticVersion.Parse(entity.Version);
    }

    async Task SetVersion(string id, SemanticVersion version)
    {
        await using var context = CreateContext();
        var entity = await context.PatchVersions.FindAsync(id);
        if (entity is null)
        {
            entity = new PatchVersion { Id = id };
            context.PatchVersions.Add(entity);
        }

        entity.Version = version.ToString();
        await context.SaveChangesAsync();
    }

    PatchesDbContext CreateContext()
    {
        var context = new PatchesDbContext(options.Value.OptionsResolver());
        if (_migrated)
        {
            return context;
        }

        try
        {
            lock (_migrationLock)
            {
                if (!_migrated)
                {
                    context.Database.Migrate();
                    _migrated = true;
                }
            }
        }
        catch
        {
            context.Dispose();
            throw;
        }

        return context;
    }
}
