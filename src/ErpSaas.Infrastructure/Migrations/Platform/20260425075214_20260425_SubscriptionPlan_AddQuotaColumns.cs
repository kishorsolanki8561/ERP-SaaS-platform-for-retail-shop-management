using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpSaas.Infrastructure.Migrations.Platform
{
    /// <inheritdoc />
    public partial class _20260425_SubscriptionPlan_AddQuotaColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "MaxUsers",
                schema: "identity",
                table: "SubscriptionPlan",
                type: "int",
                nullable: false,
                defaultValue: 5,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "EmailQuotaPerMonth",
                schema: "identity",
                table: "SubscriptionPlan",
                type: "int",
                nullable: false,
                defaultValue: 500);

            migrationBuilder.AddColumn<int>(
                name: "MaxInvoicesPerMonth",
                schema: "identity",
                table: "SubscriptionPlan",
                type: "int",
                nullable: false,
                defaultValue: 1000);

            migrationBuilder.AddColumn<int>(
                name: "MaxProducts",
                schema: "identity",
                table: "SubscriptionPlan",
                type: "int",
                nullable: false,
                defaultValue: 500);

            migrationBuilder.AddColumn<int>(
                name: "SmsQuotaPerMonth",
                schema: "identity",
                table: "SubscriptionPlan",
                type: "int",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.AddColumn<int>(
                name: "StorageQuotaMb",
                schema: "identity",
                table: "SubscriptionPlan",
                type: "int",
                nullable: false,
                defaultValue: 500);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailQuotaPerMonth",
                schema: "identity",
                table: "SubscriptionPlan");

            migrationBuilder.DropColumn(
                name: "MaxInvoicesPerMonth",
                schema: "identity",
                table: "SubscriptionPlan");

            migrationBuilder.DropColumn(
                name: "MaxProducts",
                schema: "identity",
                table: "SubscriptionPlan");

            migrationBuilder.DropColumn(
                name: "SmsQuotaPerMonth",
                schema: "identity",
                table: "SubscriptionPlan");

            migrationBuilder.DropColumn(
                name: "StorageQuotaMb",
                schema: "identity",
                table: "SubscriptionPlan");

            migrationBuilder.AlterColumn<int>(
                name: "MaxUsers",
                schema: "identity",
                table: "SubscriptionPlan",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 5);
        }
    }
}
