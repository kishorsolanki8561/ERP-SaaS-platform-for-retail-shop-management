using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Infrastructure.Sequence;
using ErpSaas.Modules.Purchasing.Entities;
using ErpSaas.Modules.Purchasing.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Purchasing.Services;

public sealed class PurchasingService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ISequenceService sequence,
    ITenantContext tenant,
    ILogger<PurchasingService> logger,
    IAutoVoucherService autoVoucher)
    : BaseService<TenantDbContext>(db, errorLogger), IPurchasingService
{
    // ── Suppliers ──────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<SupplierDto>> ListSuppliersAsync(CancellationToken ct = default)
    {
        return await db.Set<Supplier>()
            .Where(s => !s.IsDeleted)
            .Select(s => new SupplierDto(s.Id, s.Name, s.Code, s.GstNumber, s.Phone, s.Email, s.IsActive))
            .ToListAsync(ct);
    }

    public async Task<Result<long>> CreateSupplierAsync(CreateSupplierDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Purchasing.CreateSupplier", async () =>
        {
            if (dto.Code is not null &&
                await db.Set<Supplier>().AnyAsync(s => s.ShopId == tenant.ShopId && s.Code == dto.Code, ct))
                return Result<long>.Conflict(Errors.Purchasing.SupplierCodeExists);

            var supplier = new Supplier
            {
                ShopId = tenant.ShopId,
                Name = dto.Name, Code = dto.Code,
                GstNumber = dto.GstNumber, PanNumber = dto.PanNumber,
                Phone = dto.Phone, Email = dto.Email,
                Address = dto.Address, City = dto.City, State = dto.State, Pincode = dto.Pincode,
                OpeningBalance = dto.OpeningBalance, Notes = dto.Notes,
                IsActive = true, CreatedAtUtc = DateTime.UtcNow,
            };
            db.Set<Supplier>().Add(supplier);
            await db.SaveChangesAsync(ct);
            return Result<long>.Success(supplier.Id);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> UpdateSupplierAsync(long id, UpdateSupplierDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Purchasing.UpdateSupplier", async () =>
        {
            var supplier = await db.Set<Supplier>().FirstOrDefaultAsync(s => s.Id == id, ct);
            if (supplier is null) return Result<bool>.NotFound(Errors.Purchasing.SupplierNotFound);

            supplier.Name = dto.Name; supplier.GstNumber = dto.GstNumber;
            supplier.PanNumber = dto.PanNumber; supplier.Phone = dto.Phone;
            supplier.Email = dto.Email; supplier.Address = dto.Address;
            supplier.City = dto.City; supplier.State = dto.State; supplier.Pincode = dto.Pincode;
            supplier.IsActive = dto.IsActive; supplier.Notes = dto.Notes;
            supplier.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> DeleteSupplierAsync(long id, CancellationToken ct = default)
    {
        return await ExecuteAsync("Purchasing.DeleteSupplier", async () =>
        {
            var supplier = await db.Set<Supplier>().FirstOrDefaultAsync(s => s.Id == id, ct);
            if (supplier is null) return Result<bool>.NotFound(Errors.Purchasing.SupplierNotFound);

            supplier.IsDeleted = true; supplier.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    // ── Purchase Orders ────────────────────────────────────────────────────────

    public async Task<Result<long>> CreatePurchaseOrderAsync(CreatePurchaseOrderDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Purchasing.CreatePurchaseOrder", async () =>
        {
            var supplier = await db.Set<Supplier>().FirstOrDefaultAsync(s => s.Id == dto.SupplierId, ct);
            if (supplier is null) return Result<long>.NotFound(Errors.Purchasing.SupplierNotFound);

            var poNumber = await sequence.NextAsync(Constants.SequenceCodes.PurchaseOrder, tenant.ShopId, ct);

            var po = new PurchaseOrder
            {
                ShopId = tenant.ShopId,
                PoNumber = poNumber,
                SupplierId = dto.SupplierId,
                SupplierNameSnapshot = supplier.Name,
                SupplierGstSnapshot = supplier.GstNumber,
                OrderDate = dto.OrderDate,
                ExpectedDeliveryDate = dto.ExpectedDeliveryDate,
                Status = PurchaseOrderStatus.Draft,
                Notes = dto.Notes,
                BranchId = dto.BranchId,
                CreatedAtUtc = DateTime.UtcNow,
            };

            decimal subTotal = 0, totalTax = 0;
            foreach (var line in dto.Lines)
            {
                var taxable = line.QuantityInBilledUnit * line.UnitPrice * (1 - line.DiscountPercent / 100m);
                var tax = taxable * line.GstRate / 100m;
                subTotal += taxable;
                totalTax += tax;

                po.Lines.Add(new PurchaseOrderLine
                {
                    ShopId = tenant.ShopId,
                    ProductId = line.ProductId,
                    ProductNameSnapshot = "Product",
                    ProductCodeSnapshot = line.ProductId.ToString(),
                    ProductUnitId = line.ProductUnitId,
                    UnitCodeSnapshot = "PCS",
                    ConversionFactorSnapshot = 1,
                    QuantityInBilledUnit = line.QuantityInBilledUnit,
                    QuantityInBaseUnit = line.QuantityInBilledUnit,
                    UnitPrice = line.UnitPrice,
                    DiscountPercent = line.DiscountPercent,
                    DiscountAmount = line.QuantityInBilledUnit * line.UnitPrice * line.DiscountPercent / 100m,
                    TaxableAmount = taxable,
                    GstRate = line.GstRate,
                    CgstAmount = tax / 2m,
                    SgstAmount = tax / 2m,
                    IgstAmount = 0,
                    LineTotal = taxable + tax,
                    CreatedAtUtc = DateTime.UtcNow,
                });
            }
            po.SubTotal = subTotal;
            po.TotalTaxAmount = totalTax;
            po.GrandTotal = subTotal + totalTax;

            db.Set<PurchaseOrder>().Add(po);
            await db.SaveChangesAsync(ct);
            return Result<long>.Success(po.Id);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> SendPurchaseOrderAsync(long id, CancellationToken ct = default)
    {
        return await ExecuteAsync("Purchasing.SendPurchaseOrder", async () =>
        {
            var po = await db.Set<PurchaseOrder>().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (po is null) return Result<bool>.NotFound(Errors.Purchasing.PoNotFound);
            if (po.Status != PurchaseOrderStatus.Draft) return Result<bool>.Conflict(Errors.Purchasing.PoNotDraft);

            po.Status = PurchaseOrderStatus.Sent;
            po.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> ReceivePurchaseOrderAsync(ReceivePoDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Purchasing.ReceivePurchaseOrder", async () =>
        {
            var po = await db.Set<PurchaseOrder>()
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == dto.PurchaseOrderId, ct);
            if (po is null) return Result<bool>.NotFound(Errors.Purchasing.PoNotFound);
            if (po.Status == PurchaseOrderStatus.Cancelled) return Result<bool>.Conflict(Errors.Purchasing.PoAlreadyCancelled);

            foreach (var recv in dto.Lines)
            {
                var line = po.Lines.FirstOrDefault(l => l.Id == recv.LineId);
                if (line is null) continue;
                line.QuantityReceived = Math.Min(recv.QuantityReceived, line.QuantityInBilledUnit);
            }

            var allReceived = po.Lines.All(l => l.QuantityReceived >= l.QuantityInBilledUnit);
            po.Status = allReceived ? PurchaseOrderStatus.Received : PurchaseOrderStatus.PartiallyReceived;
            po.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> CancelPurchaseOrderAsync(long id, CancellationToken ct = default)
    {
        return await ExecuteAsync("Purchasing.CancelPurchaseOrder", async () =>
        {
            var po = await db.Set<PurchaseOrder>().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (po is null) return Result<bool>.NotFound(Errors.Purchasing.PoNotFound);
            if (po.Status == PurchaseOrderStatus.Cancelled) return Result<bool>.Conflict(Errors.Purchasing.PoAlreadyCancelled);

            po.Status = PurchaseOrderStatus.Cancelled;
            po.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    // ── Bills ──────────────────────────────────────────────────────────────────

    public async Task<Result<long>> CreateBillAsync(CreateBillDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Purchasing.CreateBill", async () =>
        {
            var supplier = await db.Set<Supplier>().FirstOrDefaultAsync(s => s.Id == dto.SupplierId, ct);
            if (supplier is null) return Result<long>.NotFound(Errors.Purchasing.SupplierNotFound);

            var billNumber = await sequence.NextAsync(Constants.SequenceCodes.Bill, tenant.ShopId, ct);

            var bill = new Bill
            {
                ShopId = tenant.ShopId,
                BillNumber = billNumber,
                SupplierBillNumber = dto.SupplierBillNumber,
                SupplierId = dto.SupplierId,
                SupplierNameSnapshot = supplier.Name,
                PurchaseOrderId = dto.PurchaseOrderId,
                BillDate = dto.BillDate,
                DueDate = dto.DueDate,
                Status = BillStatus.Draft,
                SubTotal = dto.SubTotal,
                TotalTaxAmount = dto.TotalTaxAmount,
                GrandTotal = dto.GrandTotal,
                PaidAmount = 0,
                OutstandingAmount = dto.GrandTotal,
                Notes = dto.Notes,
                CreatedAtUtc = DateTime.UtcNow,
            };
            db.Set<Bill>().Add(bill);
            await db.SaveChangesAsync(ct);
            return Result<long>.Success(bill.Id);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> ApproveBillAsync(long id, CancellationToken ct = default)
    {
        return await ExecuteAsync("Purchasing.ApproveBill", async () =>
        {
            var bill = await db.Set<Bill>().FirstOrDefaultAsync(b => b.Id == id, ct);
            if (bill is null) return Result<bool>.NotFound(Errors.Purchasing.BillNotFound);
            if (bill.Status != BillStatus.Draft) return Result<bool>.Conflict(Errors.Purchasing.BillNotDraft);

            bill.Status = BillStatus.Approved;
            bill.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            await autoVoucher.PostPurchaseBillVoucherAsync(tenant.ShopId, bill.Id, bill.BillNumber, bill.GrandTotal, ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> PayBillAsync(PayBillDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Purchasing.PayBill", async () =>
        {
            var bill = await db.Set<Bill>().FirstOrDefaultAsync(b => b.Id == dto.BillId, ct);
            if (bill is null) return Result<bool>.NotFound(Errors.Purchasing.BillNotFound);
            if (bill.Status == BillStatus.Cancelled) return Result<bool>.Conflict(Errors.Purchasing.BillAlreadyCancelled);
            if (bill.Status == BillStatus.Draft) return Result<bool>.Conflict(Errors.Purchasing.BillNotApproved);
            if (dto.Amount > bill.OutstandingAmount) return Result<bool>.Conflict(Errors.Purchasing.BillOverpayment);

            var payment = new BillPayment
            {
                ShopId = tenant.ShopId,
                BillId = dto.BillId,
                PaymentDate = dto.PaymentDate,
                Amount = dto.Amount,
                PaymentModeCode = dto.PaymentModeCode,
                ReferenceNumber = dto.ReferenceNumber,
                Notes = dto.Notes,
                CreatedAtUtc = DateTime.UtcNow,
            };
            db.Set<BillPayment>().Add(payment);

            bill.PaidAmount += dto.Amount;
            bill.OutstandingAmount -= dto.Amount;
            bill.Status = bill.OutstandingAmount <= 0 ? BillStatus.Paid : BillStatus.PartiallyPaid;
            bill.UpdatedAtUtc = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> CancelBillAsync(long id, CancellationToken ct = default)
    {
        return await ExecuteAsync("Purchasing.CancelBill", async () =>
        {
            var bill = await db.Set<Bill>().FirstOrDefaultAsync(b => b.Id == id, ct);
            if (bill is null) return Result<bool>.NotFound(Errors.Purchasing.BillNotFound);
            if (bill.Status == BillStatus.Cancelled) return Result<bool>.Conflict(Errors.Purchasing.BillAlreadyCancelled);

            bill.Status = BillStatus.Cancelled;
            bill.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    // ── Purchase Returns ───────────────────────────────────────────────────────

    public async Task<Result<long>> CreatePurchaseReturnAsync(CreatePurchaseReturnDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Purchasing.CreatePurchaseReturn", async () =>
        {
            var supplier = await db.Set<Supplier>().FirstOrDefaultAsync(s => s.Id == dto.SupplierId, ct);
            if (supplier is null) return Result<long>.NotFound(Errors.Purchasing.SupplierNotFound);

            string? poNumber = null;
            if (dto.PurchaseOrderId.HasValue)
            {
                var po = await db.Set<PurchaseOrder>().FirstOrDefaultAsync(p => p.Id == dto.PurchaseOrderId.Value, ct);
                if (po is null) return Result<long>.NotFound(Errors.Purchasing.PoNotFound);
                poNumber = po.PoNumber;
            }

            var returnNumber = await sequence.NextAsync(Constants.SequenceCodes.PurchaseReturn, tenant.ShopId, ct);

            var ret = new PurchaseReturn
            {
                ShopId = tenant.ShopId,
                ReturnNumber = returnNumber,
                SupplierId = dto.SupplierId,
                SupplierNameSnapshot = supplier.Name,
                PurchaseOrderId = dto.PurchaseOrderId,
                PoNumberSnapshot = poNumber,
                ReturnDate = dto.ReturnDate,
                Status = PurchaseReturnStatus.Draft,
                Reason = dto.Reason,
                Notes = dto.Notes,
                BranchId = dto.BranchId,
                CreatedAtUtc = DateTime.UtcNow,
            };

            decimal subTotal = 0, totalTax = 0;
            foreach (var l in dto.Lines)
            {
                var qtyBase = l.QuantityInBilledUnit;
                var taxable = Math.Round(l.QuantityInBilledUnit * l.UnitPrice, 2);
                var cgst = Math.Round(taxable * l.GstRate / 200, 2);
                var sgst = cgst;
                var lineTotal = taxable + cgst + sgst;
                subTotal += taxable;
                totalTax += cgst + sgst;

                ret.Lines.Add(new PurchaseReturnLine
                {
                    ShopId = tenant.ShopId,
                    ProductId = l.ProductId,
                    ProductNameSnapshot = "—",
                    ProductCodeSnapshot = "—",
                    ProductUnitId = l.ProductUnitId,
                    UnitCodeSnapshot = "—",
                    ConversionFactorSnapshot = 1,
                    QuantityInBilledUnit = l.QuantityInBilledUnit,
                    QuantityInBaseUnit = qtyBase,
                    UnitPrice = l.UnitPrice,
                    TaxableAmount = taxable,
                    GstRate = l.GstRate,
                    CgstAmount = cgst,
                    SgstAmount = sgst,
                    LineTotal = lineTotal,
                    CreatedAtUtc = DateTime.UtcNow,
                });
            }

            ret.SubTotal = subTotal;
            ret.TotalTaxAmount = totalTax;
            ret.GrandTotal = subTotal + totalTax;

            db.Set<PurchaseReturn>().Add(ret);
            await db.SaveChangesAsync(ct);
            return Result<long>.Success(ret.Id);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> ApprovePurchaseReturnAsync(long id, CancellationToken ct = default)
    {
        return await ExecuteAsync("Purchasing.ApprovePurchaseReturn", async () =>
        {
            var ret = await db.Set<PurchaseReturn>().FirstOrDefaultAsync(r => r.Id == id, ct);
            if (ret is null) return Result<bool>.NotFound(Errors.Purchasing.PurchaseReturnNotFound);
            if (ret.Status != PurchaseReturnStatus.Draft) return Result<bool>.Conflict(Errors.Purchasing.PurchaseReturnNotDraft);

            ret.Status = PurchaseReturnStatus.Approved;
            ret.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> CancelPurchaseReturnAsync(long id, CancellationToken ct = default)
    {
        return await ExecuteAsync("Purchasing.CancelPurchaseReturn", async () =>
        {
            var ret = await db.Set<PurchaseReturn>().FirstOrDefaultAsync(r => r.Id == id, ct);
            if (ret is null) return Result<bool>.NotFound(Errors.Purchasing.PurchaseReturnNotFound);
            if (ret.Status == PurchaseReturnStatus.Cancelled) return Result<bool>.Conflict(Errors.Purchasing.PurchaseReturnAlreadyCancelled);

            ret.Status = PurchaseReturnStatus.Cancelled;
            ret.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<Result<long>> IssueDebitNoteAsync(IssueDebitNoteDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Purchasing.IssueDebitNote", async () =>
        {
            var ret = await db.Set<PurchaseReturn>().FirstOrDefaultAsync(r => r.Id == dto.PurchaseReturnId, ct);
            if (ret is null) return Result<long>.NotFound(Errors.Purchasing.PurchaseReturnNotFound);
            if (ret.Status != PurchaseReturnStatus.Approved) return Result<long>.Conflict(Errors.Purchasing.PurchaseReturnNotApproved);
            if (ret.DebitNoteId.HasValue) return Result<long>.Conflict(Errors.Purchasing.DebitNoteAlreadyIssued);

            var dn = new DebitNote
            {
                ShopId = tenant.ShopId,
                DebitNoteNumber = await sequence.NextAsync(Constants.SequenceCodes.DebitNote, tenant.ShopId, ct),
                SupplierId = ret.SupplierId,
                SupplierNameSnapshot = ret.SupplierNameSnapshot,
                PurchaseReturnId = ret.Id,
                IssueDate = dto.IssueDate,
                ExpiryDate = dto.ExpiryDate,
                Status = DebitNoteStatus.Issued,
                Amount = ret.GrandTotal,
                UsedAmount = 0,
                RemainingAmount = ret.GrandTotal,
                Notes = dto.Notes,
                CreatedAtUtc = DateTime.UtcNow,
            };
            db.Set<DebitNote>().Add(dn);
            await db.SaveChangesAsync(ct);

            ret.DebitNoteId = dn.Id;
            ret.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            return Result<long>.Success(dn.Id);
        }, ct, useTransaction: true);
    }
}
