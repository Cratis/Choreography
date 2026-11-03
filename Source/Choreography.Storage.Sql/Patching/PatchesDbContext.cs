// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Choreography.Storage.Sql.Patching;

/// <summary>
/// Represents the database context for applied patches and the versions patching keeps.
/// </summary>
/// <param name="options">The <see cref="DbContextOptions"/> to use.</param>
public class PatchesDbContext(DbContextOptions options) : DbContext(options)
{
    /// <summary>
    /// Gets or sets the <see cref="DbSet{TEntity}"/> for applied patches.
    /// </summary>
    public DbSet<Patch> Patches { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="DbSet{TEntity}"/> for the versions patching keeps.
    /// </summary>
    public DbSet<PatchVersion> PatchVersions { get; set; }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Patch>().ToTable(WellKnownTableNames.Patches);
        modelBuilder.Entity<PatchVersion>().ToTable(WellKnownTableNames.PatchVersions);
    }
}
