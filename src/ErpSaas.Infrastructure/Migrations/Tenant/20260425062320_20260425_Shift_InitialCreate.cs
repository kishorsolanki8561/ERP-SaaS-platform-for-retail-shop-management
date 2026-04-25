using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpSaas.Infrastructure.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class _20260425_Shift_InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "shift");

            migrationBuilder.CreateTable(
                name: "Shift",
                schema: "shift",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    CashierUserId = table.Column<long>(type: "bigint", nullable: false),
                    CashierNameSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OpenedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OpeningCash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosingCashCounted = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SystemComputedCash = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CashVariance = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ClosingNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ForcedClosedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    TransactionCount = table.Column<int>(type: "int", nullable: false),
                    TotalSales = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCashSales = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCardSales = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalUpiSales = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalWalletDebits = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCashRefunds = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    ShopId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shift", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShiftCashMovement",
                schema: "shift",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShiftId = table.Column<long>(type: "bigint", nullable: false),
                    MovementAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReasonCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AuthorizedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    RelatedExpenseId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    ShopId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftCashMovement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftCashMovement_Shift_ShiftId",
                        column: x => x.ShiftId,
                        principalSchema: "shift",
                        principalTable: "Shift",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShiftDenominationCount",
                schema: "shift",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShiftId = table.Column<long>(type: "bigint", nullable: false),
                    Phase = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Denomination = table.Column<int>(type: "int", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    ShopId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftDenominationCount", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftDenominationCount_Shift_ShiftId",
                        column: x => x.ShiftId,
                        principalSchema: "shift",
                        principalTable: "Shift",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Shift_ShopId_CashierUserId_Status",
                schema: "shift",
                table: "Shift",
                columns: new[] { "ShopId", "CashierUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ShiftCashMovement_ShiftId",
                schema: "shift",
                table: "ShiftCashMovement",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftCashMovement_ShopId_ShiftId",
                schema: "shift",
                table: "ShiftCashMovement",
                columns: new[] { "ShopId", "ShiftId" });

            migrationBuilder.CreateIndex(
                name: "IX_ShiftDenominationCount_ShiftId",
                schema: "shift",
                table: "ShiftDenominationCount",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftDenominationCount_ShopId_ShiftId_Phase",
                schema: "shift",
                table: "ShiftDenominationCount",
                columns: new[] { "ShopId", "ShiftId", "Phase" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShiftCashMovement",
                schema: "shift");

            migrationBuilder.DropTable(
                name: "ShiftDenominationCount",
                schema: "shift");

            migrationBuilder.DropTable(
                name: "Shift",
                schema: "shift");
        }
    }
}
