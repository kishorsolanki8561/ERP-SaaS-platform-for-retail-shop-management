using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpSaas.Infrastructure.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class _20260503_Marketplace_Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "marketplace");

            migrationBuilder.CreateTable(
                name: "MarketplaceAccounts",
                schema: "marketplace",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MarketplaceCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SellerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CredentialsJsonEncrypted = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SyncInventory = table.Column<bool>(type: "bit", nullable: false),
                    SyncPricing = table.Column<bool>(type: "bit", nullable: false),
                    SyncOrders = table.Column<bool>(type: "bit", nullable: false),
                    LastSyncUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_MarketplaceAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketplaceOrders",
                schema: "marketplace",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MarketplaceAccountId = table.Column<long>(type: "bigint", nullable: false),
                    MarketplaceOrderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CustomerPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ShippingAddressJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrderTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GeneratedInvoiceId = table.Column<long>(type: "bigint", nullable: true),
                    RawPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_MarketplaceOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketplaceOrders_MarketplaceAccounts_MarketplaceAccountId",
                        column: x => x.MarketplaceAccountId,
                        principalSchema: "marketplace",
                        principalTable: "MarketplaceAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MarketplaceProductMappings",
                schema: "marketplace",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MarketplaceAccountId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    ProductVariantId = table.Column<long>(type: "bigint", nullable: true),
                    MarketplaceSku = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MarketplaceListingId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PriceOverride = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_MarketplaceProductMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketplaceProductMappings_MarketplaceAccounts_MarketplaceAccountId",
                        column: x => x.MarketplaceAccountId,
                        principalSchema: "marketplace",
                        principalTable: "MarketplaceAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceAccounts_ShopId_MarketplaceCode_SellerId",
                schema: "marketplace",
                table: "MarketplaceAccounts",
                columns: new[] { "ShopId", "MarketplaceCode", "SellerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceOrders_MarketplaceAccountId",
                schema: "marketplace",
                table: "MarketplaceOrders",
                column: "MarketplaceAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceOrders_ShopId_MarketplaceAccountId_MarketplaceOrderId",
                schema: "marketplace",
                table: "MarketplaceOrders",
                columns: new[] { "ShopId", "MarketplaceAccountId", "MarketplaceOrderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceProductMappings_MarketplaceAccountId",
                schema: "marketplace",
                table: "MarketplaceProductMappings",
                column: "MarketplaceAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceProductMappings_ShopId_MarketplaceAccountId_MarketplaceSku",
                schema: "marketplace",
                table: "MarketplaceProductMappings",
                columns: new[] { "ShopId", "MarketplaceAccountId", "MarketplaceSku" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketplaceOrders",
                schema: "marketplace");

            migrationBuilder.DropTable(
                name: "MarketplaceProductMappings",
                schema: "marketplace");

            migrationBuilder.DropTable(
                name: "MarketplaceAccounts",
                schema: "marketplace");
        }
    }
}
