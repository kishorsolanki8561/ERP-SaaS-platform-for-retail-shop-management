using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.Billing.Services;

public enum PdfFormat { A4, Thermal80mm }

public interface IInvoicePdfGenerator
{
    byte[] Generate(InvoiceDetailDto invoice, ShopInfoSnapshot? shop, PdfFormat format);
}
