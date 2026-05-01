using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpSaas.Infrastructure.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class _20260428_Accounting_Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "accounting");

            migrationBuilder.CreateTable(
                name: "AccountGroup",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ParentId = table.Column<long>(type: "bigint", nullable: true),
                    Nature = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_AccountGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountGroup_AccountGroup_ParentId",
                        column: x => x.ParentId,
                        principalSchema: "accounting",
                        principalTable: "AccountGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancialYear",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StartYear = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedByUserId = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_FinancialYear", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Account",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AccountGroupId = table.Column<long>(type: "bigint", nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OpeningBalanceType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    GstNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    LinkedCustomerId = table.Column<long>(type: "bigint", nullable: true),
                    LinkedSupplierId = table.Column<long>(type: "bigint", nullable: true),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_Account", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Account_AccountGroup_AccountGroupId",
                        column: x => x.AccountGroupId,
                        principalSchema: "accounting",
                        principalTable: "AccountGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Voucher",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VoucherNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VoucherDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VoucherType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Narration = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TotalDebit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCredit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SourceDocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SourceDocumentId = table.Column<long>(type: "bigint", nullable: true),
                    IsPosted = table.Column<bool>(type: "bit", nullable: false),
                    PostedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinancialYearId = table.Column<long>(type: "bigint", nullable: true),
                    ReversedByVoucherId = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_Voucher", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Voucher_FinancialYear_FinancialYearId",
                        column: x => x.FinancialYearId,
                        principalSchema: "accounting",
                        principalTable: "FinancialYear",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BankAccount",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IfscCode = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    BranchName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AccountHolderName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_BankAccount", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankAccount_Account_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "accounting",
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Expense",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpenseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentModeCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PaidFromAccountId = table.Column<long>(type: "bigint", nullable: true),
                    AttachmentFileId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VoucherId = table.Column<long>(type: "bigint", nullable: true),
                    IsRecurring = table.Column<bool>(type: "bit", nullable: false),
                    RecurrenceInterval = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
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
                    table.PrimaryKey("PK_Expense", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Expense_Account_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "accounting",
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Expense_Voucher_VoucherId",
                        column: x => x.VoucherId,
                        principalSchema: "accounting",
                        principalTable: "Voucher",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VoucherEntry",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VoucherId = table.Column<long>(type: "bigint", nullable: false),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Narration = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_VoucherEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VoucherEntry_Account_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "accounting",
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VoucherEntry_Voucher_VoucherId",
                        column: x => x.VoucherId,
                        principalSchema: "accounting",
                        principalTable: "Voucher",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Account_AccountGroupId",
                schema: "accounting",
                table: "Account",
                column: "AccountGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Account_ShopId_Code",
                schema: "accounting",
                table: "Account",
                columns: new[] { "ShopId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountGroup_ParentId",
                schema: "accounting",
                table: "AccountGroup",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountGroup_ShopId_Code",
                schema: "accounting",
                table: "AccountGroup",
                columns: new[] { "ShopId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankAccount_AccountId",
                schema: "accounting",
                table: "BankAccount",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccount_ShopId_AccountNumber",
                schema: "accounting",
                table: "BankAccount",
                columns: new[] { "ShopId", "AccountNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expense_AccountId",
                schema: "accounting",
                table: "Expense",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Expense_ShopId_ExpenseDate",
                schema: "accounting",
                table: "Expense",
                columns: new[] { "ShopId", "ExpenseDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Expense_VoucherId",
                schema: "accounting",
                table: "Expense",
                column: "VoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialYear_ShopId_StartYear",
                schema: "accounting",
                table: "FinancialYear",
                columns: new[] { "ShopId", "StartYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Voucher_FinancialYearId",
                schema: "accounting",
                table: "Voucher",
                column: "FinancialYearId");

            migrationBuilder.CreateIndex(
                name: "IX_Voucher_ShopId_VoucherNumber",
                schema: "accounting",
                table: "Voucher",
                columns: new[] { "ShopId", "VoucherNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VoucherEntry_AccountId",
                schema: "accounting",
                table: "VoucherEntry",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_VoucherEntry_ShopId_AccountId",
                schema: "accounting",
                table: "VoucherEntry",
                columns: new[] { "ShopId", "AccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_VoucherEntry_ShopId_VoucherId",
                schema: "accounting",
                table: "VoucherEntry",
                columns: new[] { "ShopId", "VoucherId" });

            migrationBuilder.CreateIndex(
                name: "IX_VoucherEntry_VoucherId",
                schema: "accounting",
                table: "VoucherEntry",
                column: "VoucherId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankAccount",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "Expense",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "VoucherEntry",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "Account",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "Voucher",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "AccountGroup",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "FinancialYear",
                schema: "accounting");
        }
    }
}
