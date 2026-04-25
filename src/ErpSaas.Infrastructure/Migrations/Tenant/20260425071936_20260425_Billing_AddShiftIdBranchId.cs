using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpSaas.Infrastructure.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class _20260425_Billing_AddShiftIdBranchId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "BranchId",
                schema: "sales",
                table: "Invoice",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ShiftId",
                schema: "sales",
                table: "Invoice",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_ShopId_ShiftId",
                schema: "sales",
                table: "Invoice",
                columns: new[] { "ShopId", "ShiftId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoice_ShopId_ShiftId",
                schema: "sales",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "BranchId",
                schema: "sales",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "ShiftId",
                schema: "sales",
                table: "Invoice");
        }
    }
}
