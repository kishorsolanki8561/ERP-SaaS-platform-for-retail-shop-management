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
            migrationBuilder.DropColumn(
                name: "ExpiresAtUtc",
                schema: "verticals_medical",
                table: "PrescriptionRecord");

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                schema: "verticals",
                table: "ShopVertical",
                type: "varbinary(max)",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                schema: "service",
                table: "ServiceJobPart",
                type: "varbinary(max)",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                schema: "service",
                table: "ServiceJobLabor",
                type: "varbinary(max)",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                schema: "service",
                table: "ServiceJob",
                type: "varbinary(max)",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceivedAtDate",
                schema: "service",
                table: "ServiceJob",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                schema: "verticals_medical",
                table: "PrescriptionRecord",
                type: "varbinary(max)",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                schema: "verticals_grocery",
                table: "LoyaltyTransaction",
                type: "varbinary(max)",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                schema: "verticals_grocery",
                table: "LoyaltyProgram",
                type: "varbinary(max)",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                schema: "verticals_medical",
                table: "DrugBatch",
                type: "varbinary(max)",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceJobPart_ServiceJobId",
                schema: "service",
                table: "ServiceJobPart",
                column: "ServiceJobId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceJobLabor_ServiceJobId",
                schema: "service",
                table: "ServiceJobLabor",
                column: "ServiceJobId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionRecord_DrugBatchId",
                schema: "verticals_medical",
                table: "PrescriptionRecord",
                column: "DrugBatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceJobPart_ServiceJobId",
                schema: "service",
                table: "ServiceJobPart");

            migrationBuilder.DropIndex(
                name: "IX_ServiceJobLabor_ServiceJobId",
                schema: "service",
                table: "ServiceJobLabor");

            migrationBuilder.DropIndex(
                name: "IX_PrescriptionRecord_DrugBatchId",
                schema: "verticals_medical",
                table: "PrescriptionRecord");

            migrationBuilder.DropColumn(
                name: "ReceivedAtDate",
                schema: "service",
                table: "ServiceJob");

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                schema: "verticals",
                table: "ShopVertical",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)");

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                schema: "service",
                table: "ServiceJobPart",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)");

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                schema: "service",
                table: "ServiceJobLabor",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)");

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                schema: "service",
                table: "ServiceJob",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)");

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                schema: "verticals_medical",
                table: "PrescriptionRecord",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAtUtc",
                schema: "verticals_medical",
                table: "PrescriptionRecord",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                schema: "verticals_grocery",
                table: "LoyaltyTransaction",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)");

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                schema: "verticals_grocery",
                table: "LoyaltyProgram",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)");

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                schema: "verticals_medical",
                table: "DrugBatch",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)");
        }
    }
}
