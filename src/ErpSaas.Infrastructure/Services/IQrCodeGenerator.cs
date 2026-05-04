namespace ErpSaas.Infrastructure.Services;

public sealed record UpiQrRequest(
    string VirtualPaymentAddress,   // e.g. shop@upi
    string PayeeName,
    decimal Amount,
    string TransactionRef,          // invoice number / reference
    string? TransactionNote = null,
    int SizePx = 256);

public sealed record QrCodeResult(
    string Base64Png,               // data:image/png;base64,...
    string UpiString);              // full upi:// URI — useful for deep-links

public interface IQrCodeGenerator
{
    Task<QrCodeResult> GenerateUpiQrAsync(UpiQrRequest request, CancellationToken ct = default);
}
