using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpSaas.Infrastructure.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class _20260504_Payment_ConnectorFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentUrl",
                schema: "payment",
                table: "PaymentGatewayTransaction",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundGatewayTxnId",
                schema: "payment",
                table: "PaymentGatewayTransaction",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentUrl",
                schema: "payment",
                table: "PaymentGatewayTransaction");

            migrationBuilder.DropColumn(
                name: "RefundGatewayTxnId",
                schema: "payment",
                table: "PaymentGatewayTransaction");
        }
    }
}
