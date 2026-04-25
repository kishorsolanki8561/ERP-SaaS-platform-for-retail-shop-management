using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpSaas.Infrastructure.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class _20260425_Billing_AddPaymentsAndCreditTerms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                schema: "sales",
                table: "Invoice",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OutstandingAmount",
                schema: "sales",
                table: "Invoice",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                schema: "sales",
                table: "Invoice",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PaymentTerms",
                schema: "sales",
                table: "Invoice",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InvoicePayment",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<long>(type: "bigint", nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_InvoicePayment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoicePayment_Invoice_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "sales",
                        principalTable: "Invoice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoicePayment_InvoiceId",
                schema: "sales",
                table: "InvoicePayment",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoicePayment_ShopId_InvoiceId",
                schema: "sales",
                table: "InvoicePayment",
                columns: new[] { "ShopId", "InvoiceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoicePayment",
                schema: "sales");

            migrationBuilder.DropColumn(
                name: "DueDate",
                schema: "sales",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "OutstandingAmount",
                schema: "sales",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "PaidAmount",
                schema: "sales",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "PaymentTerms",
                schema: "sales",
                table: "Invoice");
        }
    }
}
