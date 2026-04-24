using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Infrastructure.MultiTenant;

/// <summary>
/// Declares all TenantDB schemas so EF migrations create them upfront.
/// Business modules add their entity configs on top of these.
/// </summary>
public static class TenantSchemaRegistry
{
    /// <summary>All schemas that exist inside TenantDB.</summary>
    public static readonly IReadOnlyList<string> AllSchemas =
    [
        "sales",        // Invoice, SalesOrder, Quotation, DeliveryChallan, SalesReturn, CreditNote
        "inventory",    // Product, StockMovement, StockCount, Warehouse, ProductUnit
        "purchasing",   // PurchaseOrder, GoodsReceipt, PurchaseReturn, Supplier
        "accounting",   // Voucher, JournalEntry, LedgerAccount, CostCenter
        "wallet",       // WalletLedger, WalletTransaction, PaymentGatewayOrder
        "warranty",     // WarrantyClaim, WarrantyPolicy, ServiceCenter
        "hr",           // Employee, Attendance, Leave, Salary, Designation
        "crm",          // Customer, CustomerAddress, CustomerGroup, LeadSource
        "pricing",      // PriceList, PriceListLine, SpecialPrice, DiscountRule
        "transport",    // DeliveryRoute, Courier, TrackingEvent
        "marketplace",  // MarketplaceListing, MarketplaceOrder, WebhookPayload
        "barcode",      // BarcodeLabel, BarcodePrint, BarcodeMapping
        "reporting",    // ReportDefinition, ReportSchedule, ReportExport
        "notifications",// (also in NotificationsDB — tenant overrides)
        "masters",      // DdlItemTenant (already declared)
        "menu",         // MenuItemTenantOverride (already declared)
        "files",        // UploadedFile (already declared)
        "sequence",     // SequenceDefinition (already declared)
    ];
}
