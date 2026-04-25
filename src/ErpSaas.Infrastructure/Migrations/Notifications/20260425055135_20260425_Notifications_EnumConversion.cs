using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpSaas.Infrastructure.Migrations.Notifications
{
    /// <inheritdoc />
    public partial class _20260425_Notifications_EnumConversion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NotificationQueue_Status_NextRetryAtUtc",
                schema: "notifications",
                table: "NotificationQueue");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationQueue_Status",
                schema: "notifications",
                table: "NotificationQueue",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NotificationQueue_Status",
                schema: "notifications",
                table: "NotificationQueue");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationQueue_Status_NextRetryAtUtc",
                schema: "notifications",
                table: "NotificationQueue",
                columns: new[] { "Status", "NextRetryAtUtc" });
        }
    }
}
