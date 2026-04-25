using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpSaas.Infrastructure.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class _20260425_Metering_InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "metering");

            migrationBuilder.CreateTable(
                name: "UsageEvent",
                schema: "metering",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MeterCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Delta = table.Column<long>(type: "bigint", nullable: false),
                    SourceEntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SourceEntityId = table.Column<long>(type: "bigint", nullable: true),
                    TriggeredByUserId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    ShopId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageEvent", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UsageMeter",
                schema: "metering",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MeterCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PeriodStartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEndUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Used = table.Column<long>(type: "bigint", nullable: false),
                    Quota = table.Column<long>(type: "bigint", nullable: false),
                    HardCapEnforced = table.Column<bool>(type: "bit", nullable: false),
                    OverageCount = table.Column<long>(type: "bigint", nullable: false),
                    OverageChargeRate = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    ShopId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageMeter", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UsageEvent_ShopId_MeterCode_OccurredAtUtc",
                schema: "metering",
                table: "UsageEvent",
                columns: new[] { "ShopId", "MeterCode", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UsageMeter_ShopId_MeterCode_PeriodStartUtc",
                schema: "metering",
                table: "UsageMeter",
                columns: new[] { "ShopId", "MeterCode", "PeriodStartUtc" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsageEvent",
                schema: "metering");

            migrationBuilder.DropTable(
                name: "UsageMeter",
                schema: "metering");
        }
    }
}
