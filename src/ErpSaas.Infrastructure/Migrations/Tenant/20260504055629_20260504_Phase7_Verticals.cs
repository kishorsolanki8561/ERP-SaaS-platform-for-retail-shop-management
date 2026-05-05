using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpSaas.Infrastructure.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class _20260504_Phase7_Verticals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "service");
            migrationBuilder.EnsureSchema(name: "verticals");
            migrationBuilder.EnsureSchema(name: "verticals_grocery");
            migrationBuilder.EnsureSchema(name: "verticals_medical");

            // ── service.ServiceJob ────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "ServiceJob",
                schema: "service",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ShopId = table.Column<long>(type: "bigint", nullable: false),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    CustomerNameSnapshot = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CustomerPhoneSnapshot = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ProductId = table.Column<long>(type: "bigint", nullable: true),
                    ItemDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReportedIssue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DiagnosisNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsUnderWarranty = table.Column<bool>(type: "bit", nullable: false),
                    WarrantyRegistrationId = table.Column<long>(type: "bigint", nullable: true),
                    AssignedTechnicianUserId = table.Column<long>(type: "bigint", nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ActualLaborCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ActualPartsCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReceivedAtDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DiagnosedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedByCustomerAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResultingInvoiceId = table.Column<long>(type: "bigint", nullable: true),
                    ResultingWarrantyClaimId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceJob", x => x.Id);
                });

            // ── service.ServiceJobLabor ────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "ServiceJobLabor",
                schema: "service",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceJobId = table.Column<long>(type: "bigint", nullable: false),
                    ShopId = table.Column<long>(type: "bigint", nullable: false),
                    TechnicianUserId = table.Column<long>(type: "bigint", nullable: false),
                    TechnicianNameSnapshot = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Hours = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    HourlyRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LaborCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceJobLabor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceJobLabor_ServiceJob_ServiceJobId",
                        column: x => x.ServiceJobId,
                        principalSchema: "service",
                        principalTable: "ServiceJob",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ── service.ServiceJobPart ─────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "ServiceJobPart",
                schema: "service",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceJobId = table.Column<long>(type: "bigint", nullable: false),
                    ShopId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    ProductNameSnapshot = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StockMovementId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceJobPart", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceJobPart_ServiceJob_ServiceJobId",
                        column: x => x.ServiceJobId,
                        principalSchema: "service",
                        principalTable: "ServiceJob",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ── verticals.ShopVertical ────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "ShopVertical",
                schema: "verticals",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShopId = table.Column<long>(type: "bigint", nullable: false),
                    VerticalPackId = table.Column<long>(type: "bigint", nullable: false),
                    VerticalPackCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AppliedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AppliedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopVertical", x => x.Id);
                });

            // ── verticals_grocery.LoyaltyProgram ──────────────────────────────
            migrationBuilder.CreateTable(
                name: "LoyaltyProgram",
                schema: "verticals_grocery",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShopId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PointsPerRupee = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    RupeeValuePerPoint = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    MinimumRedemptionPoints = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxRedemptionPercentPerBill = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    PointExpiryDays = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyProgram", x => x.Id);
                });

            // ── verticals_grocery.LoyaltyTransaction ──────────────────────────
            migrationBuilder.CreateTable(
                name: "LoyaltyTransaction",
                schema: "verticals_grocery",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShopId = table.Column<long>(type: "bigint", nullable: false),
                    LoyaltyProgramId = table.Column<long>(type: "bigint", nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    InvoiceId = table.Column<long>(type: "bigint", nullable: true),
                    TransactionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Points = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyTransaction", x => x.Id);
                });

            // ── verticals_medical.DrugBatch ───────────────────────────────────
            migrationBuilder.CreateTable(
                name: "DrugBatch",
                schema: "verticals_medical",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShopId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    ProductNameSnapshot = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    BatchNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GenericName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Manufacturer = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SupplierNameSnapshot = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Schedule = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ManufactureDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InitialQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CurrentQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SellingPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PurchaseBillId = table.Column<long>(type: "bigint", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrugBatch", x => x.Id);
                });

            // ── verticals_medical.PrescriptionRecord ──────────────────────────
            migrationBuilder.CreateTable(
                name: "PrescriptionRecord",
                schema: "verticals_medical",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShopId = table.Column<long>(type: "bigint", nullable: false),
                    DrugBatchId = table.Column<long>(type: "bigint", nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    InvoiceId = table.Column<long>(type: "bigint", nullable: false),
                    DoctorName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    DoctorRegistrationNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PrescriptionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuantityDispensed = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    FileId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionRecord", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrescriptionRecord_DrugBatch_DrugBatchId",
                        column: x => x.DrugBatchId,
                        principalSchema: "verticals_medical",
                        principalTable: "DrugBatch",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // ── Indexes: ServiceJob ────────────────────────────────────────────
            migrationBuilder.CreateIndex(
                name: "IX_ServiceJob_ShopId_CustomerId",
                schema: "service",
                table: "ServiceJob",
                columns: new[] { "ShopId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceJob_ShopId_JobNumber",
                schema: "service",
                table: "ServiceJob",
                columns: new[] { "ShopId", "JobNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceJob_ShopId_Status",
                schema: "service",
                table: "ServiceJob",
                columns: new[] { "ShopId", "Status" });

            // ── Indexes: ServiceJobLabor ───────────────────────────────────────
            migrationBuilder.CreateIndex(
                name: "IX_ServiceJobLabor_ServiceJobId",
                schema: "service",
                table: "ServiceJobLabor",
                column: "ServiceJobId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceJobLabor_ShopId_ServiceJobId",
                schema: "service",
                table: "ServiceJobLabor",
                columns: new[] { "ShopId", "ServiceJobId" });

            // ── Indexes: ServiceJobPart ────────────────────────────────────────
            migrationBuilder.CreateIndex(
                name: "IX_ServiceJobPart_ServiceJobId",
                schema: "service",
                table: "ServiceJobPart",
                column: "ServiceJobId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceJobPart_ShopId_ServiceJobId",
                schema: "service",
                table: "ServiceJobPart",
                columns: new[] { "ShopId", "ServiceJobId" });

            // ── Indexes: ShopVertical ─────────────────────────────────────────
            migrationBuilder.CreateIndex(
                name: "IX_ShopVertical_ShopId",
                schema: "verticals",
                table: "ShopVertical",
                column: "ShopId",
                unique: true);

            // ── Indexes: LoyaltyProgram ───────────────────────────────────────
            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyProgram_ShopId",
                schema: "verticals_grocery",
                table: "LoyaltyProgram",
                column: "ShopId",
                unique: true);

            // ── Indexes: LoyaltyTransaction ───────────────────────────────────
            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTransaction_ShopId_CustomerId",
                schema: "verticals_grocery",
                table: "LoyaltyTransaction",
                columns: new[] { "ShopId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTransaction_ShopId_InvoiceId",
                schema: "verticals_grocery",
                table: "LoyaltyTransaction",
                columns: new[] { "ShopId", "InvoiceId" });

            // ── Indexes: DrugBatch ────────────────────────────────────────────
            migrationBuilder.CreateIndex(
                name: "IX_DrugBatch_ShopId_ExpiryDate",
                schema: "verticals_medical",
                table: "DrugBatch",
                columns: new[] { "ShopId", "ExpiryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DrugBatch_ShopId_ProductId",
                schema: "verticals_medical",
                table: "DrugBatch",
                columns: new[] { "ShopId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_DrugBatch_ShopId_ProductId_BatchNumber",
                schema: "verticals_medical",
                table: "DrugBatch",
                columns: new[] { "ShopId", "ProductId", "BatchNumber" },
                unique: true);

            // ── Indexes: PrescriptionRecord ───────────────────────────────────
            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionRecord_DrugBatchId",
                schema: "verticals_medical",
                table: "PrescriptionRecord",
                column: "DrugBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionRecord_ShopId_DrugBatchId",
                schema: "verticals_medical",
                table: "PrescriptionRecord",
                columns: new[] { "ShopId", "DrugBatchId" });

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionRecord_ShopId_InvoiceId",
                schema: "verticals_medical",
                table: "PrescriptionRecord",
                columns: new[] { "ShopId", "InvoiceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PrescriptionRecord", schema: "verticals_medical");
            migrationBuilder.DropTable(name: "ServiceJobLabor",    schema: "service");
            migrationBuilder.DropTable(name: "ServiceJobPart",     schema: "service");
            migrationBuilder.DropTable(name: "LoyaltyTransaction", schema: "verticals_grocery");
            migrationBuilder.DropTable(name: "ShopVertical",       schema: "verticals");
            migrationBuilder.DropTable(name: "DrugBatch",          schema: "verticals_medical");
            migrationBuilder.DropTable(name: "LoyaltyProgram",     schema: "verticals_grocery");
            migrationBuilder.DropTable(name: "ServiceJob",         schema: "service");
        }
    }
}
