using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpSaas.Infrastructure.Migrations.Platform
{
    /// <inheritdoc />
    public partial class _20260504_Platform_OnPremReplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "replication");

            migrationBuilder.CreateTable(
                name: "ChangeTrackingLog",
                schema: "replication",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EntityId = table.Column<long>(type: "bigint", nullable: false),
                    Operation = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PatchJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VersionNumber = table.Column<long>(type: "bigint", nullable: false),
                    OriginDeploymentId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    ShopId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeTrackingLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConflictArchive",
                schema: "replication",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeploymentId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EntityId = table.Column<long>(type: "bigint", nullable: false),
                    CloudSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OnPremSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Strategy = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ResolutionNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ResolvedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    ShopId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConflictArchive", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OnPremDeployment",
                schema: "replication",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeploymentId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ShopLocalEndpoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PublicKey = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    InstalledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastReplicationAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LastFullReplicationAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SoftwareVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    ShopId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnPremDeployment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReplicationLog",
                schema: "replication",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeploymentId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Direction = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RowsTransferred = table.Column<int>(type: "int", nullable: false),
                    RowsConflicted = table.Column<int>(type: "int", nullable: false),
                    RowsFailed = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ErrorSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PayloadBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    ShopId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplicationLog", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChangeTrackingLog_ShopId_EntityName_EntityId",
                schema: "replication",
                table: "ChangeTrackingLog",
                columns: new[] { "ShopId", "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChangeTrackingLog_VersionNumber",
                schema: "replication",
                table: "ChangeTrackingLog",
                column: "VersionNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ConflictArchive_DeploymentId",
                schema: "replication",
                table: "ConflictArchive",
                column: "DeploymentId");

            migrationBuilder.CreateIndex(
                name: "IX_ConflictArchive_ShopId_Outcome",
                schema: "replication",
                table: "ConflictArchive",
                columns: new[] { "ShopId", "Outcome" });

            migrationBuilder.CreateIndex(
                name: "IX_OnPremDeployment_ShopId",
                schema: "replication",
                table: "OnPremDeployment",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_OnPremDeployment_ShopId_DeploymentId",
                schema: "replication",
                table: "OnPremDeployment",
                columns: new[] { "ShopId", "DeploymentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReplicationLog_ShopId_DeploymentId",
                schema: "replication",
                table: "ReplicationLog",
                columns: new[] { "ShopId", "DeploymentId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplicationLog_StartedAtUtc",
                schema: "replication",
                table: "ReplicationLog",
                column: "StartedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChangeTrackingLog",
                schema: "replication");

            migrationBuilder.DropTable(
                name: "ConflictArchive",
                schema: "replication");

            migrationBuilder.DropTable(
                name: "OnPremDeployment",
                schema: "replication");

            migrationBuilder.DropTable(
                name: "ReplicationLog",
                schema: "replication");
        }
    }
}
