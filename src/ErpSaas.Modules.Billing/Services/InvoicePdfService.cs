using ErpSaas.Shared.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ErpSaas.Modules.Billing.Services;

public sealed class InvoicePdfGenerator : IInvoicePdfGenerator
{
    public byte[] Generate(InvoiceDetailDto invoice, ShopInfoSnapshot? shop, PdfFormat format)
        => format == PdfFormat.A4
            ? GenerateA4(invoice, shop)
            : GenerateThermal80mm(invoice, shop);

    // ── A4 ────────────────────────────────────────────────────────────────────

    private static byte[] GenerateA4(InvoiceDetailDto invoice, ShopInfoSnapshot? shop)
        => Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontSize(10));

                page.Header().Column(header =>
                {
                    header.Item().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(shop?.TradeName ?? shop?.LegalName ?? "Shop")
                                .Bold().FontSize(18);
                            if (shop?.GstNumber is not null)
                                col.Item().Text($"GSTIN: {shop.GstNumber}").FontSize(9);
                            var addr = BuildAddress(shop);
                            if (addr is not null)
                                col.Item().Text(addr).FontSize(9).FontColor(Colors.Grey.Darken2);
                        });

                        row.ConstantItem(120).Column(col =>
                        {
                            col.Item().Text("INVOICE").Bold().FontSize(16)
                                .AlignRight();
                            col.Item().Text($"# {invoice.InvoiceNumber}").Bold().AlignRight();
                            col.Item().Text(invoice.InvoiceDate.ToString("dd-MMM-yyyy")).AlignRight();
                            col.Item().Text(invoice.Status.ToString()).FontColor(Colors.Grey.Darken1)
                                .AlignRight();
                        });
                    });

                    header.Item().PaddingVertical(4)
                        .LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().Column(col =>
                {
                    col.Item().PaddingVertical(6).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Bill To:").Bold().FontSize(9).FontColor(Colors.Grey.Darken2);
                            c.Item().Text(invoice.CustomerName).Bold();
                            if (invoice.Lines.Count > 0)
                                c.Item().Text($"Customer #{invoice.CustomerId}").FontSize(9);
                        });
                    });

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(28);
                            cols.RelativeColumn(3);
                            cols.ConstantColumn(50);
                            cols.ConstantColumn(70);
                            cols.ConstantColumn(45);
                            cols.ConstantColumn(75);
                        });

                        table.Header(h =>
                        {
                            static IContainer CellStyle(IContainer c) =>
                                c.DefaultTextStyle(t => t.Bold().FontSize(9))
                                 .Background(Colors.Grey.Lighten3)
                                 .Padding(5);

                            h.Cell().Element(CellStyle).Text("#");
                            h.Cell().Element(CellStyle).Text("Product");
                            h.Cell().Element(CellStyle).AlignRight().Text("Qty");
                            h.Cell().Element(CellStyle).AlignRight().Text("Rate");
                            h.Cell().Element(CellStyle).AlignRight().Text("Disc%");
                            h.Cell().Element(CellStyle).AlignRight().Text("Total");
                        });

                        var altRow = false;
                        foreach (var (line, idx) in invoice.Lines.Select((l, i) => (l, i + 1)))
                        {
                            altRow = !altRow;
                            var bg = altRow ? Colors.White : Colors.Grey.Lighten5;

                            static IContainer RowCell(IContainer c, string bg) =>
                                c.Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5);

                            table.Cell().Element(c => RowCell(c, bg)).Text(idx.ToString()).FontSize(9);
                            table.Cell().Element(c => RowCell(c, bg)).Text(line.ProductName).FontSize(9);
                            table.Cell().Element(c => RowCell(c, bg)).AlignRight()
                                .Text($"{line.Qty} {line.UnitCode}").FontSize(9);
                            table.Cell().Element(c => RowCell(c, bg)).AlignRight()
                                .Text($"₹{line.UnitPrice:F2}").FontSize(9);
                            table.Cell().Element(c => RowCell(c, bg)).AlignRight()
                                .Text($"{line.DiscountPercent:F1}%").FontSize(9);
                            table.Cell().Element(c => RowCell(c, bg)).AlignRight()
                                .Text($"₹{line.LineTotal:F2}").FontSize(9);
                        }
                    });

                    col.Item().PaddingTop(8).Column(totals =>
                    {
                        static void TotalRow(ColumnDescriptor col, string label, decimal value, bool bold = false)
                        {
                            col.Item().Row(row =>
                            {
                                row.RelativeItem();
                                row.ConstantItem(150).Row(r =>
                                {
                                    var lStyle = r.RelativeItem().Text(label).FontSize(10);
                                    if (bold) lStyle.Bold();
                                    var vStyle = r.ConstantItem(90).AlignRight()
                                        .Text($"₹{value:F2}").FontSize(10);
                                    if (bold) vStyle.Bold();
                                });
                            });
                        }

                        totals.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                        TotalRow(totals, "Sub Total", invoice.SubTotal);
                        if (invoice.TotalDiscount > 0)
                            TotalRow(totals, "Discount", -invoice.TotalDiscount);
                        TotalRow(totals, "Tax", invoice.TotalTaxAmount);
                        totals.Item().LineHorizontal(1).LineColor(Colors.Grey.Darken1);
                        TotalRow(totals, "Grand Total", invoice.GrandTotal, bold: true);
                    });
                });

                page.Footer().Row(row =>
                {
                    row.RelativeItem().Text("Thank you for your business!")
                        .FontSize(9).FontColor(Colors.Grey.Darken2);
                    row.ConstantItem(60).AlignRight().Text(text =>
                    {
                        text.Span("Page ").FontSize(8);
                        text.CurrentPageNumber().FontSize(8);
                        text.Span(" of ").FontSize(8);
                        text.TotalPages().FontSize(8);
                    });
                });
            });
        }).GeneratePdf();

    // ── 80 mm Thermal ─────────────────────────────────────────────────────────

    private static byte[] GenerateThermal80mm(InvoiceDetailDto invoice, ShopInfoSnapshot? shop)
        => Document.Create(container =>
        {
            container.Page(page =>
            {
                // 80 mm wide; height auto-expands with content
                page.Size(80, 297, Unit.Millimetre);
                page.Margin(3, Unit.Millimetre);
                page.DefaultTextStyle(t => t.FontSize(9));

                page.Content().Column(col =>
                {
                    col.Item().AlignCenter().Text(shop?.TradeName ?? shop?.LegalName ?? "Shop")
                        .Bold().FontSize(11);
                    if (shop?.GstNumber is not null)
                        col.Item().AlignCenter().Text($"GSTIN: {shop.GstNumber}").FontSize(8);
                    var addr = BuildAddress(shop);
                    if (addr is not null)
                        col.Item().AlignCenter().Text(addr).FontSize(8);

                    col.Item().PaddingVertical(2).LineHorizontal(0.5f);

                    col.Item().Text($"Invoice: {invoice.InvoiceNumber}").Bold();
                    col.Item().Text($"Date:    {invoice.InvoiceDate:dd-MMM-yyyy}");
                    col.Item().Text($"To:      {invoice.CustomerName}");

                    col.Item().PaddingVertical(2).LineHorizontal(0.5f);

                    foreach (var line in invoice.Lines)
                    {
                        col.Item().Text(line.ProductName).Bold();
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"  {line.Qty} {line.UnitCode} x ₹{line.UnitPrice:F2}");
                            row.ConstantItem(50).AlignRight().Text($"₹{line.LineTotal:F2}");
                        });
                    }

                    col.Item().PaddingVertical(2).LineHorizontal(0.5f);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Sub Total");
                        row.ConstantItem(60).AlignRight().Text($"₹{invoice.SubTotal:F2}");
                    });
                    if (invoice.TotalDiscount > 0)
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Discount");
                            row.ConstantItem(60).AlignRight().Text($"-₹{invoice.TotalDiscount:F2}");
                        });
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Tax");
                        row.ConstantItem(60).AlignRight().Text($"₹{invoice.TotalTaxAmount:F2}");
                    });

                    col.Item().LineHorizontal(0.5f);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("TOTAL").Bold();
                        row.ConstantItem(60).AlignRight().Text($"₹{invoice.GrandTotal:F2}").Bold();
                    });

                    col.Item().PaddingVertical(4).AlignCenter()
                        .Text("Thank you!").FontSize(8).FontColor(Colors.Grey.Darken2);
                });
            });
        }).GeneratePdf();

    private static string? BuildAddress(ShopInfoSnapshot? shop)
    {
        if (shop is null) return null;
        var parts = new[] { shop.AddressLine1, shop.AddressLine2, shop.City, shop.StateCode, shop.PinCode }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        var addr = string.Join(", ", parts);
        return string.IsNullOrWhiteSpace(addr) ? null : addr;
    }
}
