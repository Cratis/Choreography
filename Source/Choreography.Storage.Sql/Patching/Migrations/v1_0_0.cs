// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Choreography.Storage.Sql.Patching.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

/// <summary>
/// Creates the schema the SQL patch storage reads and writes.
/// </summary>
/// <remarks>
/// The column helpers come from Cratis.Arc.EntityFrameworkCore so a single migration is valid across SQL Server, PostgreSQL
/// and SQLite. It shares the migration history table with the jobs migrations; each migration names the context it belongs to.
/// </remarks>
[DbContext(typeof(PatchesDbContext))]
[Migration($"Cratis-Choreography-Patches-{nameof(v1_0_0)}")]
public class v1_0_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.Patches,
            columns: table => new
            {
                Name = table.StringColumn(migrationBuilder, maxLength: 256),
                Version = table.StringColumn(migrationBuilder, maxLength: 256),
                AppliedAt = table.Column<DateTimeOffset>(nullable: false),
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.Patches}", x => x.Name));

        migrationBuilder.CreateTable(
            name: WellKnownTableNames.PatchVersions,
            columns: table => new
            {
                Id = table.StringColumn(migrationBuilder, maxLength: 64),
                Version = table.StringColumn(migrationBuilder, maxLength: 256),
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.PatchVersions}", x => x.Id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: WellKnownTableNames.PatchVersions);
        migrationBuilder.DropTable(name: WellKnownTableNames.Patches);
    }
}
