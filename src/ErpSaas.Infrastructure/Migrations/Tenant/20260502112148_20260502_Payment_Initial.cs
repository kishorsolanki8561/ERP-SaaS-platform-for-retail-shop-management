using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpSaas.Infrastructure.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class _20260502_Payment_Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "payment");

            migrationBuilder.CreateTable(
                name: "PaymentGatewayAccount",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GatewayCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CredentialsJsonEncrypted = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    WebhookSecretEncrypted = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_PaymentGatewayAccount", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentGatewayTransaction",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GatewayCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    GatewayTxnId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OurReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SourceInvoiceId = table.Column<long>(type: "bigint", nullable: true),
                    SourceWalletTopUpId = table.Column<long>(type: "bigint", nullable: true),
                    SourceSubscriptionInvoiceId = table.Column<long>(type: "bigint", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Method = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Vpa = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CardLast4 = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FailureCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FailureMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InitiatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GatewayFee = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    GatewayGst = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    NetSettled = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SettledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SettlementReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ThirdPartyApiLogId = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_PaymentGatewayTransaction", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReconciliationException",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GatewayCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    GatewayTxnId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OurReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PaymentGatewayTransactionId = table.Column<long>(type: "bigint", nullable: true),
                    ExceptionType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OurAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    GatewayAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    OurFee = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    GatewayFee = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DetectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_ReconciliationException", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentGatewayAccount_ShopId_GatewayCode",
                schema: "payment",
                table: "PaymentGatewayAccount",
                columns: new[] { "ShopId", "GatewayCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentGatewayTransaction_ShopId_GatewayCode_GatewayTxnId",
                schema: "payment",
                table: "PaymentGatewayTransaction",
                columns: new[] { "ShopId", "GatewayCode", "GatewayTxnId" },
                unique: true,
                filter: "[GatewayTxnId] != ''");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentGatewayTransaction_ShopId_InitiatedAtUtc",
                schema: "payment",
                table: "PaymentGatewayTransaction",
                columns: new[] { "ShopId", "InitiatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentGatewayTransaction_ShopId_OurReferenceNumber",
                schema: "payment",
                table: "PaymentGatewayTransaction",
                columns: new[] { "ShopId", "OurReferenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentGatewayTransaction_ShopId_Purpose",
                schema: "payment",
                table: "PaymentGatewayTransaction",
                columns: new[] { "ShopId", "Purpose" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentGatewayTransaction_ShopId_Status",
                schema: "payment",
                table: "PaymentGatewayTransaction",
                columns: new[] { "ShopId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationException_ShopId_GatewayCode_DetectedAtUtc",
                schema: "payment",
                table: "ReconciliationException",
                columns: new[] { "ShopId", "GatewayCode", "DetectedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationException_ShopId_Status",
                schema: "payment",
                table: "ReconciliationException",
                columns: new[] { "ShopId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentGatewayAccount",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "PaymentGatewayTransaction",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "ReconciliationException",
                schema: "payment");
        }
    }
}
