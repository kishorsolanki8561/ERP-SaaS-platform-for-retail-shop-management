using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpSaas.Infrastructure.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class _20260504_Shared_BaseEntityDeleteTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "warranty",
                table: "WarrantyRegistration",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "warranty",
                table: "WarrantyRegistration",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "warranty",
                table: "WarrantyClaim",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "warranty",
                table: "WarrantyClaim",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "inventory",
                table: "Warehouse",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "inventory",
                table: "Warehouse",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "wallet",
                table: "WalletTransaction",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "wallet",
                table: "WalletTransaction",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "wallet",
                table: "WalletTopUp",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "wallet",
                table: "WalletTopUp",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "wallet",
                table: "WalletBalance",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "wallet",
                table: "WalletBalance",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "accounting",
                table: "VoucherEntry",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "accounting",
                table: "VoucherEntry",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "accounting",
                table: "Voucher",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "accounting",
                table: "Voucher",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "transport",
                table: "Vehicle",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "transport",
                table: "Vehicle",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "metering",
                table: "UsageMeter",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "metering",
                table: "UsageMeter",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "metering",
                table: "UsageEvent",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "metering",
                table: "UsageEvent",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "files",
                table: "UploadedFile",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "files",
                table: "UploadedFile",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "transport",
                table: "TransportProvider",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "transport",
                table: "TransportProvider",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "purchasing",
                table: "Supplier",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "purchasing",
                table: "Supplier",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "inventory",
                table: "StockMovement",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "inventory",
                table: "StockMovement",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "hr",
                table: "StaffActivities",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "hr",
                table: "StaffActivities",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "shift",
                table: "ShiftDenominationCount",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "shift",
                table: "ShiftDenominationCount",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "shift",
                table: "ShiftCashMovement",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "shift",
                table: "ShiftCashMovement",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "shift",
                table: "Shift",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "shift",
                table: "Shift",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "sequence",
                table: "SequenceDefinition",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "sequence",
                table: "SequenceDefinition",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "sales",
                table: "SalesReturnLine",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "sales",
                table: "SalesReturnLine",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "sales",
                table: "SalesReturn",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "sales",
                table: "SalesReturn",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "sales",
                table: "SalesOrderLine",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "sales",
                table: "SalesOrderLine",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "sales",
                table: "SalesOrder",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "sales",
                table: "SalesOrder",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "hr",
                table: "SalaryComponents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "hr",
                table: "SalaryComponents",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "accounting",
                table: "ReconciliationRule",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "accounting",
                table: "ReconciliationRule",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "payment",
                table: "ReconciliationException",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "payment",
                table: "ReconciliationException",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "hardware",
                table: "ReceiptTemplate",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "hardware",
                table: "ReceiptTemplate",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "sales",
                table: "QuotationLine",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "sales",
                table: "QuotationLine",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "sales",
                table: "Quotation",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "sales",
                table: "Quotation",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "purchasing",
                table: "PurchaseReturnLine",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "purchasing",
                table: "PurchaseReturnLine",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "purchasing",
                table: "PurchaseReturn",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "purchasing",
                table: "PurchaseReturn",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "purchasing",
                table: "PurchaseOrderLine",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "purchasing",
                table: "PurchaseOrderLine",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "purchasing",
                table: "PurchaseOrder",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "purchasing",
                table: "PurchaseOrder",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "inventory",
                table: "ProductUnit",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "inventory",
                table: "ProductUnit",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "inventory",
                table: "Product",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "inventory",
                table: "Product",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "accounting",
                table: "PettyCashClosure",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "accounting",
                table: "PettyCashClosure",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "hr",
                table: "Payrolls",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "hr",
                table: "Payrolls",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "payment",
                table: "PaymentGatewayTransaction",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "payment",
                table: "PaymentGatewayTransaction",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "payment",
                table: "PaymentGatewayAccount",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "payment",
                table: "PaymentGatewayAccount",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "portal",
                table: "OnlineOrderLine",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "portal",
                table: "OnlineOrderLine",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "portal",
                table: "OnlineOrder",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "portal",
                table: "OnlineOrder",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "pricing",
                table: "Offer",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "pricing",
                table: "Offer",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "menu",
                table: "MenuItemTenantOverride",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "menu",
                table: "MenuItemTenantOverride",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "marketplace",
                table: "MarketplaceProductMappings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "marketplace",
                table: "MarketplaceProductMappings",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "marketplace",
                table: "MarketplaceOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "marketplace",
                table: "MarketplaceOrders",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "marketplace",
                table: "MarketplaceAccounts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "marketplace",
                table: "MarketplaceAccounts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "hr",
                table: "LeaveTypes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "hr",
                table: "LeaveTypes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "hr",
                table: "LeaveRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "hr",
                table: "LeaveRequests",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "hr",
                table: "LeaveBalances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "hr",
                table: "LeaveBalances",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "hardware",
                table: "LabelTemplate",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "hardware",
                table: "LabelTemplate",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "sales",
                table: "InvoicePayment",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "sales",
                table: "InvoicePayment",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "sales",
                table: "InvoiceLine",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "sales",
                table: "InvoiceLine",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "sales",
                table: "Invoice",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "sales",
                table: "Invoice",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "accounting",
                table: "FixedAsset",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "accounting",
                table: "FixedAsset",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "accounting",
                table: "FinancialYear",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "accounting",
                table: "FinancialYear",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "pricing",
                table: "ExtraChargeRule",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "pricing",
                table: "ExtraChargeRule",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "accounting",
                table: "Expense",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "accounting",
                table: "Expense",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "hr",
                table: "Employees",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "hr",
                table: "Employees",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "hr",
                table: "EmployeeDocuments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "hr",
                table: "EmployeeDocuments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "pricing",
                table: "DiscountRule",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "pricing",
                table: "DiscountRule",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "hardware",
                table: "DeviceProfile",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "hardware",
                table: "DeviceProfile",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "accounting",
                table: "DepreciationEntry",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "accounting",
                table: "DepreciationEntry",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "transport",
                table: "DeliveryLog",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "transport",
                table: "DeliveryLog",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "sales",
                table: "DeliveryChallanLine",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "sales",
                table: "DeliveryChallanLine",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "sales",
                table: "DeliveryChallan",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "sales",
                table: "DeliveryChallan",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "transport",
                table: "Delivery",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "transport",
                table: "Delivery",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "purchasing",
                table: "DebitNote",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "purchasing",
                table: "DebitNote",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "masters",
                table: "DdlItemTenant",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "masters",
                table: "DdlItemTenant",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "portal",
                table: "CustomerInquiryMessage",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "portal",
                table: "CustomerInquiryMessage",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "portal",
                table: "CustomerInquiry",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "portal",
                table: "CustomerInquiry",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "crm",
                table: "CustomerGroup",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "crm",
                table: "CustomerGroup",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "crm",
                table: "CustomerAddress",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "crm",
                table: "CustomerAddress",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "crm",
                table: "Customer",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "crm",
                table: "Customer",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "sales",
                table: "CreditNote",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "sales",
                table: "CreditNote",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "accounting",
                table: "Cheque",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "accounting",
                table: "Cheque",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "purchasing",
                table: "BillPayment",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "purchasing",
                table: "BillPayment",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "purchasing",
                table: "Bill",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "purchasing",
                table: "Bill",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "accounting",
                table: "BankStatementLine",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "accounting",
                table: "BankStatementLine",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "accounting",
                table: "BankStatement",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "accounting",
                table: "BankStatement",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "accounting",
                table: "BankAccount",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "accounting",
                table: "BankAccount",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "hr",
                table: "Attendance",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "hr",
                table: "Attendance",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "accounting",
                table: "AccountGroup",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "accounting",
                table: "AccountGroup",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "accounting",
                table: "Account",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedByUserId",
                schema: "accounting",
                table: "Account",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "warranty",
                table: "WarrantyRegistration");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "warranty",
                table: "WarrantyRegistration");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "warranty",
                table: "WarrantyClaim");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "warranty",
                table: "WarrantyClaim");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "inventory",
                table: "Warehouse");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "inventory",
                table: "Warehouse");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "wallet",
                table: "WalletTransaction");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "wallet",
                table: "WalletTransaction");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "wallet",
                table: "WalletTopUp");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "wallet",
                table: "WalletTopUp");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "wallet",
                table: "WalletBalance");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "wallet",
                table: "WalletBalance");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "accounting",
                table: "VoucherEntry");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "accounting",
                table: "VoucherEntry");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "accounting",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "accounting",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "transport",
                table: "Vehicle");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "transport",
                table: "Vehicle");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "metering",
                table: "UsageMeter");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "metering",
                table: "UsageMeter");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "metering",
                table: "UsageEvent");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "metering",
                table: "UsageEvent");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "files",
                table: "UploadedFile");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "files",
                table: "UploadedFile");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "transport",
                table: "TransportProvider");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "transport",
                table: "TransportProvider");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "purchasing",
                table: "Supplier");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "purchasing",
                table: "Supplier");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "inventory",
                table: "StockMovement");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "inventory",
                table: "StockMovement");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "hr",
                table: "StaffActivities");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "hr",
                table: "StaffActivities");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "shift",
                table: "ShiftDenominationCount");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "shift",
                table: "ShiftDenominationCount");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "shift",
                table: "ShiftCashMovement");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "shift",
                table: "ShiftCashMovement");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "shift",
                table: "Shift");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "shift",
                table: "Shift");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "sequence",
                table: "SequenceDefinition");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "sequence",
                table: "SequenceDefinition");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "sales",
                table: "SalesReturnLine");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "sales",
                table: "SalesReturnLine");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "sales",
                table: "SalesReturn");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "sales",
                table: "SalesReturn");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "sales",
                table: "SalesOrderLine");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "sales",
                table: "SalesOrderLine");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "sales",
                table: "SalesOrder");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "sales",
                table: "SalesOrder");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "hr",
                table: "SalaryComponents");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "hr",
                table: "SalaryComponents");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "accounting",
                table: "ReconciliationRule");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "accounting",
                table: "ReconciliationRule");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "payment",
                table: "ReconciliationException");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "payment",
                table: "ReconciliationException");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "hardware",
                table: "ReceiptTemplate");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "hardware",
                table: "ReceiptTemplate");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "sales",
                table: "QuotationLine");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "sales",
                table: "QuotationLine");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "sales",
                table: "Quotation");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "sales",
                table: "Quotation");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "purchasing",
                table: "PurchaseReturnLine");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "purchasing",
                table: "PurchaseReturnLine");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "purchasing",
                table: "PurchaseReturn");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "purchasing",
                table: "PurchaseReturn");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "purchasing",
                table: "PurchaseOrderLine");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "purchasing",
                table: "PurchaseOrderLine");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "purchasing",
                table: "PurchaseOrder");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "purchasing",
                table: "PurchaseOrder");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "inventory",
                table: "ProductUnit");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "inventory",
                table: "ProductUnit");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "inventory",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "inventory",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "accounting",
                table: "PettyCashClosure");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "accounting",
                table: "PettyCashClosure");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "hr",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "hr",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "payment",
                table: "PaymentGatewayTransaction");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "payment",
                table: "PaymentGatewayTransaction");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "payment",
                table: "PaymentGatewayAccount");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "payment",
                table: "PaymentGatewayAccount");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "portal",
                table: "OnlineOrderLine");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "portal",
                table: "OnlineOrderLine");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "portal",
                table: "OnlineOrder");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "portal",
                table: "OnlineOrder");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "pricing",
                table: "Offer");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "pricing",
                table: "Offer");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "menu",
                table: "MenuItemTenantOverride");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "menu",
                table: "MenuItemTenantOverride");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "marketplace",
                table: "MarketplaceProductMappings");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "marketplace",
                table: "MarketplaceProductMappings");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "marketplace",
                table: "MarketplaceOrders");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "marketplace",
                table: "MarketplaceOrders");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "marketplace",
                table: "MarketplaceAccounts");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "marketplace",
                table: "MarketplaceAccounts");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "hr",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "hr",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "hr",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "hr",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "hr",
                table: "LeaveBalances");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "hr",
                table: "LeaveBalances");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "hardware",
                table: "LabelTemplate");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "hardware",
                table: "LabelTemplate");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "sales",
                table: "InvoicePayment");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "sales",
                table: "InvoicePayment");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "sales",
                table: "InvoiceLine");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "sales",
                table: "InvoiceLine");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "sales",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "sales",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "accounting",
                table: "FixedAsset");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "accounting",
                table: "FixedAsset");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "accounting",
                table: "FinancialYear");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "accounting",
                table: "FinancialYear");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "pricing",
                table: "ExtraChargeRule");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "pricing",
                table: "ExtraChargeRule");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "accounting",
                table: "Expense");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "accounting",
                table: "Expense");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "hr",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "hr",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "hr",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "hr",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "pricing",
                table: "DiscountRule");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "pricing",
                table: "DiscountRule");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "hardware",
                table: "DeviceProfile");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "hardware",
                table: "DeviceProfile");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "accounting",
                table: "DepreciationEntry");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "accounting",
                table: "DepreciationEntry");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "transport",
                table: "DeliveryLog");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "transport",
                table: "DeliveryLog");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "sales",
                table: "DeliveryChallanLine");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "sales",
                table: "DeliveryChallanLine");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "sales",
                table: "DeliveryChallan");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "sales",
                table: "DeliveryChallan");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "transport",
                table: "Delivery");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "transport",
                table: "Delivery");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "purchasing",
                table: "DebitNote");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "purchasing",
                table: "DebitNote");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "masters",
                table: "DdlItemTenant");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "masters",
                table: "DdlItemTenant");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "portal",
                table: "CustomerInquiryMessage");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "portal",
                table: "CustomerInquiryMessage");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "portal",
                table: "CustomerInquiry");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "portal",
                table: "CustomerInquiry");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "crm",
                table: "CustomerGroup");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "crm",
                table: "CustomerGroup");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "crm",
                table: "CustomerAddress");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "crm",
                table: "CustomerAddress");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "crm",
                table: "Customer");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "crm",
                table: "Customer");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "sales",
                table: "CreditNote");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "sales",
                table: "CreditNote");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "accounting",
                table: "Cheque");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "accounting",
                table: "Cheque");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "purchasing",
                table: "BillPayment");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "purchasing",
                table: "BillPayment");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "purchasing",
                table: "Bill");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "purchasing",
                table: "Bill");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "accounting",
                table: "BankStatementLine");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "accounting",
                table: "BankStatementLine");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "accounting",
                table: "BankStatement");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "accounting",
                table: "BankStatement");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "accounting",
                table: "BankAccount");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "accounting",
                table: "BankAccount");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "hr",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "hr",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "accounting",
                table: "AccountGroup");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "accounting",
                table: "AccountGroup");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "accounting",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "accounting",
                table: "Account");
        }
    }
}
