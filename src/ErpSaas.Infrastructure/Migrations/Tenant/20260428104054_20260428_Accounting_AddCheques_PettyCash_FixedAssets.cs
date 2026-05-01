using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpSaas.Infrastructure.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class _20260428_Accounting_AddCheques_PettyCash_FixedAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cheque",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Direction = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ChequeNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ChequeDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceivedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BankAccountId = table.Column<long>(type: "bigint", nullable: false),
                    DrawerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DrawerBankName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DepositedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClearedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BouncedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BounceReasonCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    VoucherIdOnReceive = table.Column<long>(type: "bigint", nullable: true),
                    VoucherIdOnClear = table.Column<long>(type: "bigint", nullable: true),
                    VoucherIdOnBounce = table.Column<long>(type: "bigint", nullable: true),
                    RelatedInvoiceId = table.Column<long>(type: "bigint", nullable: true),
                    RelatedSupplierBillId = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_Cheque", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cheque_BankAccount_BankAccountId",
                        column: x => x.BankAccountId,
                        principalSchema: "accounting",
                        principalTable: "BankAccount",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FixedAsset",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CategoryCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PurchaseCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SupplierId = table.Column<long>(type: "bigint", nullable: true),
                    PurchaseInvoiceFileId = table.Column<long>(type: "bigint", nullable: true),
                    Method = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UsefulLifeYears = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    SalvageValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RateOfDepreciation = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    AccumulatedDepreciation = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetBookValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DisposalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DisposalValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    LocationNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AssignedToEmployeeId = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_FixedAsset", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PettyCashClosure",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClosureDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpectedBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CountedBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Variance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Narration = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    VarianceVoucherId = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_PettyCashClosure", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DepreciationEntry",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FixedAssetId = table.Column<long>(type: "bigint", nullable: false),
                    PeriodDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AccumulatedAfter = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetBookValueAfter = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VoucherId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_DepreciationEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepreciationEntry_FixedAsset_FixedAssetId",
                        column: x => x.FixedAssetId,
                        principalSchema: "accounting",
                        principalTable: "FixedAsset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cheque_BankAccountId",
                schema: "accounting",
                table: "Cheque",
                column: "BankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Cheque_ShopId_ChequeNumber_BankAccountId",
                schema: "accounting",
                table: "Cheque",
                columns: new[] { "ShopId", "ChequeNumber", "BankAccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cheque_ShopId_Status",
                schema: "accounting",
                table: "Cheque",
                columns: new[] { "ShopId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DepreciationEntry_FixedAssetId_PeriodDate",
                schema: "accounting",
                table: "DepreciationEntry",
                columns: new[] { "FixedAssetId", "PeriodDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FixedAsset_ShopId_AssetCode",
                schema: "accounting",
                table: "FixedAsset",
                columns: new[] { "ShopId", "AssetCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FixedAsset_ShopId_Status",
                schema: "accounting",
                table: "FixedAsset",
                columns: new[] { "ShopId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashClosure_ShopId_ClosureDate",
                schema: "accounting",
                table: "PettyCashClosure",
                columns: new[] { "ShopId", "ClosureDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cheque",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "DepreciationEntry",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "PettyCashClosure",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "FixedAsset",
                schema: "accounting");
        }
    }
}
