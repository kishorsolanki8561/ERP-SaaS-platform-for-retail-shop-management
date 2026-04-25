using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpSaas.Infrastructure.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class _20260425_Shift_AddCashierPhone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CashierPhoneSnapshot",
                schema: "shift",
                table: "Shift",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CashierPhoneSnapshot",
                schema: "shift",
                table: "Shift");
        }
    }
}
