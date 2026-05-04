using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpSaas.Infrastructure.Migrations.Platform
{
    /// <inheritdoc />
    public partial class _20260503_Marketing_LeadCapture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "marketing");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "identity",
                table: "UserShop",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "identity",
                table: "UserShop",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "identity",
                table: "UserSecurityToken",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "identity",
                table: "UserSecurityToken",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "identity",
                table: "UserRole",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "identity",
                table: "UserRole",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "identity",
                table: "User",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "identity",
                table: "User",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "identity",
                table: "SubscriptionPlanFeature",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "identity",
                table: "SubscriptionPlanFeature",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "identity",
                table: "SubscriptionPlan",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "identity",
                table: "SubscriptionPlan",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "masters",
                table: "State",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "masters",
                table: "State",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "identity",
                table: "ShopSubscription",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "identity",
                table: "ShopSubscription",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "identity",
                table: "Shop",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "identity",
                table: "Shop",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "identity",
                table: "RolePermission",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "identity",
                table: "RolePermission",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "identity",
                table: "Role",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "identity",
                table: "Role",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "identity",
                table: "Permission",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "identity",
                table: "Permission",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "menu",
                table: "MenuItem",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "menu",
                table: "MenuItem",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "masters",
                table: "HsnSacCode",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "masters",
                table: "HsnSacCode",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "masters",
                table: "Currency",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "masters",
                table: "Currency",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "masters",
                table: "Country",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "masters",
                table: "Country",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "masters",
                table: "City",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "masters",
                table: "City",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "identity",
                table: "Branch",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "identity",
                table: "Branch",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BlogPost",
                schema: "marketing",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AuthorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_BlogPost", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lead",
                schema: "marketing",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BusinessName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CityCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StateCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    VerticalCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ShopsCount = table.Column<int>(type: "int", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UtmSource = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UtmCampaign = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ConvertedShopId = table.Column<long>(type: "bigint", nullable: true),
                    AssignedUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastContactedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_Lead", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketingContent",
                schema: "marketing",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Locale = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_MarketingContent", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlogPost_IsPublished",
                schema: "marketing",
                table: "BlogPost",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPost_Slug",
                schema: "marketing",
                table: "BlogPost",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lead_AssignedUserId",
                schema: "marketing",
                table: "Lead",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Lead_Status",
                schema: "marketing",
                table: "Lead",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingContent_Key_Locale",
                schema: "marketing",
                table: "MarketingContent",
                columns: new[] { "Key", "Locale" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlogPost",
                schema: "marketing");

            migrationBuilder.DropTable(
                name: "Lead",
                schema: "marketing");

            migrationBuilder.DropTable(
                name: "MarketingContent",
                schema: "marketing");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "identity",
                table: "UserShop");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "identity",
                table: "UserShop");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "identity",
                table: "UserSecurityToken");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "identity",
                table: "UserSecurityToken");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "identity",
                table: "UserRole");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "identity",
                table: "UserRole");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "identity",
                table: "User");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "identity",
                table: "User");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "identity",
                table: "SubscriptionPlanFeature");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "identity",
                table: "SubscriptionPlanFeature");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "identity",
                table: "SubscriptionPlan");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "identity",
                table: "SubscriptionPlan");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "masters",
                table: "State");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "masters",
                table: "State");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "identity",
                table: "ShopSubscription");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "identity",
                table: "ShopSubscription");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "identity",
                table: "Shop");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "identity",
                table: "Shop");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "identity",
                table: "RolePermission");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "identity",
                table: "RolePermission");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "identity",
                table: "Role");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "identity",
                table: "Role");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "identity",
                table: "Permission");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "identity",
                table: "Permission");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "menu",
                table: "MenuItem");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "menu",
                table: "MenuItem");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "masters",
                table: "HsnSacCode");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "masters",
                table: "HsnSacCode");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "masters",
                table: "Currency");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "masters",
                table: "Currency");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "masters",
                table: "Country");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "masters",
                table: "Country");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "masters",
                table: "City");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "masters",
                table: "City");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "identity",
                table: "Branch");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "identity",
                table: "Branch");
        }
    }
}
