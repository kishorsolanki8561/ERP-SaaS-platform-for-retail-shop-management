using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpSaas.Infrastructure.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class _20260504_Crm_RowVersionConcurrencyFix : Migration
    {
        // SQL Server does not permit ALTER COLUMN on a rowversion/timestamp column.
        // The only way to change the type is drop + re-add the column.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // crm.CustomerGroup
            migrationBuilder.DropColumn(name: "RowVersion", schema: "crm", table: "CustomerGroup");
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "crm",
                table: "CustomerGroup",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);

            // crm.CustomerAddress
            migrationBuilder.DropColumn(name: "RowVersion", schema: "crm", table: "CustomerAddress");
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "crm",
                table: "CustomerAddress",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);

            // crm.Customer
            migrationBuilder.DropColumn(name: "RowVersion", schema: "crm", table: "Customer");
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "crm",
                table: "Customer",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // crm.CustomerGroup
            migrationBuilder.DropColumn(name: "RowVersion", schema: "crm", table: "CustomerGroup");
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "crm",
                table: "CustomerGroup",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            // crm.CustomerAddress
            migrationBuilder.DropColumn(name: "RowVersion", schema: "crm", table: "CustomerAddress");
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "crm",
                table: "CustomerAddress",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            // crm.Customer
            migrationBuilder.DropColumn(name: "RowVersion", schema: "crm", table: "Customer");
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "crm",
                table: "Customer",
                type: "rowversion",
                rowVersion: true,
                nullable: false);
        }
    }
}
