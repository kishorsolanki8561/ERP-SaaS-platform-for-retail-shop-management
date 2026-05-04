using ErpSaas.Infrastructure.Services;
using System.Text;

namespace ErpSaas.Tests.Unit.Infrastructure;

[Trait("Category", "Unit")]
public sealed class QrCodeGeneratorTests
{
    private readonly QrCodeGeneratorService _sut = new();

    [Fact]
    public async Task GenerateUpiQrAsync_ProducesValidBase64Png()
    {
        var req = new UpiQrRequest("merchant@upi", "Test Shop", 500m, "INV-001", null, 300);

        var result = await _sut.GenerateUpiQrAsync(req);

        Assert.False(string.IsNullOrEmpty(result.Base64Png));
        Assert.StartsWith("data:image/png;base64,", result.Base64Png);

        var base64 = result.Base64Png["data:image/png;base64,".Length..];
        var bytes = Convert.FromBase64String(base64);
        Assert.True(bytes.Length > 0);
        // PNG magic bytes: 89 50 4E 47
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal(0x50, bytes[1]);
        Assert.Equal(0x4E, bytes[2]);
        Assert.Equal(0x47, bytes[3]);
    }

    [Fact]
    public async Task GenerateUpiQrAsync_UpiStringContainsCorrectAmount()
    {
        var req = new UpiQrRequest("shop@hdfc", "Demo Shop", 1234.56m, "TXN-ABC", null, 300);

        var result = await _sut.GenerateUpiQrAsync(req);

        Assert.Contains("am=1234.56", result.UpiString);
    }

    [Fact]
    public async Task GenerateUpiQrAsync_UpiStringContainsVpa()
    {
        var req = new UpiQrRequest("kishore@okicici", "Kishore Store", 100m, "TXN-1", null, 300);

        var result = await _sut.GenerateUpiQrAsync(req);

        Assert.StartsWith("upi://pay?", result.UpiString);
        Assert.Contains("pa=kishore%40okicici", result.UpiString);
    }

    [Fact]
    public async Task GenerateUpiQrAsync_UpiStringContainsPayeeName()
    {
        var req = new UpiQrRequest("test@upi", "My Store Name", 50m, "REF-1", null, 300);

        var result = await _sut.GenerateUpiQrAsync(req);

        Assert.Contains("pn=", result.UpiString);
        Assert.Contains("cu=INR", result.UpiString);
    }

    [Fact]
    public async Task GenerateUpiQrAsync_WithTransactionNote_IncludesNote()
    {
        var req = new UpiQrRequest("pay@upi", "Shop", 200m, "INV-99", "Payment for Invoice 99", 300);

        var result = await _sut.GenerateUpiQrAsync(req);

        Assert.Contains("tn=", result.UpiString);
    }

    [Fact]
    public async Task GenerateUpiQrAsync_WithoutNote_DoesNotIncludeTn()
    {
        var req = new UpiQrRequest("pay@upi", "Shop", 200m, "INV-99", null, 300);

        var result = await _sut.GenerateUpiQrAsync(req);

        Assert.DoesNotContain("tn=", result.UpiString);
    }
}
