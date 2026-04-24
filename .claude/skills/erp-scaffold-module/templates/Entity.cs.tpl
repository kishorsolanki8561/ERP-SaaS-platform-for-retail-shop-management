using ErpSaas.Shared.Data;
using ErpSaas.Shared.Auditing;

namespace ErpSaas.Modules.{Module}.Entities;

/// <summary>
/// {Plain-English one-line description of what this represents.}
/// Plan reference: §{X.Y}.{sub}
/// </summary>
[Auditable]     // any change lands in AuditLog automatically. Remove only if Never-Auditable (e.g., view-only projections).
public class {EntityName} : TenantEntity
{
    // ── Natural key (unique within ShopId) ───────────────────────────────
    // Example: public string InvoiceNumber { get; set; } = default!;

    // ── Core fields ──────────────────────────────────────────────────────
    // public long CustomerId { get; set; }              // FK to sales.Customer
    // public DateTime InvoiceDate { get; set; }
    // public string StatusCode { get; set; } = default!;   // DDL INVOICE_STATUS

    // ── Money ────────────────────────────────────────────────────────────
    // public decimal GrandTotal { get; set; }          // ALWAYS decimal for currency, never double/float

    // ── Snapshot columns (§1 data-richness principle) ─────────────────────
    // Capture these when the source might change later but this row must
    // stay historically correct:
    // public string CustomerNameSnapshot { get; set; } = default!;

    // ── Quantity-bearing fields (§6.3.c multi-unit rules) ─────────────────
    // If this entity records a quantity, these four are REQUIRED:
    // public long? ProductUnitId { get; set; }
    // public string UnitCodeSnapshot { get; set; } = default!;
    // public decimal ConversionFactorSnapshot { get; set; } = 1m;
    // public decimal QuantityInBilledUnit { get; set; }
    // public decimal QuantityInBaseUnit { get; set; }

    // Notes/free text at the end, nullable:
    // public string? Notes { get; set; }

    // ── Inherited from TenantEntity (do NOT redeclare) ───────────────────
    //   long Id
    //   long ShopId                    (set by SaveChangesInterceptor; filtered globally)
    //   DateTime CreatedAtUtc
    //   long CreatedByUserId
    //   DateTime? UpdatedAtUtc
    //   long? UpdatedByUserId
    //   bool IsDeleted                 (soft delete — always use)
    //   byte[] RowVersion              (optimistic concurrency)
}
