// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage.Sql.Patching;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Choreography.Storage.Sql;

/// <summary>
/// Represents the options for <see cref="SqlPatchStorage"/>.
/// </summary>
public class SqlPatchStorageOptions
{
    /// <summary>
    /// Gets or sets the function that resolves the <see cref="DbContextOptions"/> for the <see cref="PatchesDbContext"/>.
    /// </summary>
    /// <remarks>
    /// The resolver decides both which database holds the patch records and which EF Core provider is used - SQL Server,
    /// PostgreSQL or SQLite. Settable rather than init-only because <c lang="csharp">AddCratisChoreographySqlPatchStorage</c>
    /// configures it through an options callback, which runs against an already-constructed instance.
    /// </remarks>
    public required Func<DbContextOptions<PatchesDbContext>> OptionsResolver { get; set; }
}
