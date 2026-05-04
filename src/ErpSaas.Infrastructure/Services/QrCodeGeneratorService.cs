using QRCoder;

namespace ErpSaas.Infrastructure.Services;

public sealed class QrCodeGeneratorService : IQrCodeGenerator
{
    public Task<QrCodeResult> GenerateUpiQrAsync(UpiQrRequest request, CancellationToken ct = default)
    {
        var upiString = BuildUpiString(request);

        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(upiString, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        var pngBytes = qrCode.GetGraphic(request.SizePx / 25); // pixelsPerModule

        var base64 = $"data:image/png;base64,{Convert.ToBase64String(pngBytes)}";
        return Task.FromResult(new QrCodeResult(base64, upiString));
    }

    private static string BuildUpiString(UpiQrRequest req)
    {
        var parts = new List<string>
        {
            $"pa={Uri.EscapeDataString(req.VirtualPaymentAddress)}",
            $"pn={Uri.EscapeDataString(req.PayeeName)}",
            $"am={req.Amount:F2}",
            $"tr={Uri.EscapeDataString(req.TransactionRef)}",
            "cu=INR",
        };
        if (!string.IsNullOrEmpty(req.TransactionNote))
            parts.Add($"tn={Uri.EscapeDataString(req.TransactionNote)}");

        return "upi://pay?" + string.Join("&", parts);
    }
}
