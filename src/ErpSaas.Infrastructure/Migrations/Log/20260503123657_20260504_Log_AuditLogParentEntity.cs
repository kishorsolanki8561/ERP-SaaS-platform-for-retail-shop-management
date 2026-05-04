using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpSaas.Infrastructure.Migrations.Log
{
    /// <inheritdoc />
    public partial class _20260504_Log_AuditLogParentEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ParentEntityId",
                schema: "log",
                table: "AuditLog",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentEntityName",
                schema: "log",
                table: "AuditLog",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_EntityName_EntityId",
                schema: "log",
                table: "AuditLog",
                columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_OccurredAtUtc",
                schema: "log",
                table: "AuditLog",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_ParentEntityName_ParentEntityId",
                schema: "log",
                table: "AuditLog",
                columns: new[] { "ParentEntityName", "ParentEntityId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditLog_EntityName_EntityId",
                schema: "log",
                table: "AuditLog");

            migrationBuilder.DropIndex(
                name: "IX_AuditLog_OccurredAtUtc",
                schema: "log",
                table: "AuditLog");

            migrationBuilder.DropIndex(
                name: "IX_AuditLog_ParentEntityName_ParentEntityId",
                schema: "log",
                table: "AuditLog");

            migrationBuilder.DropColumn(
                name: "ParentEntityId",
                schema: "log",
                table: "AuditLog");

            migrationBuilder.DropColumn(
                name: "ParentEntityName",
                schema: "log",
                table: "AuditLog");
        }
    }
}
