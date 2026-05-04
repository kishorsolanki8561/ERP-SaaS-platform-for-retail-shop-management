using ErpSaas.Modules.ApiAccess.Services;

namespace ErpSaas.Tests.Unit.Modules.ApiAccess;

[Trait("Category", "Unit")]
public sealed class WebhookSignatureGeneratorTests
{
    private readonly WebhookSignatureGenerator _sut = new();

    [Fact]
    public void Generate_SameInputs_ReturnsSameSignature()
    {
        var sig1 = _sut.Generate("{\"event\":\"test\"}", "secret123");
        var sig2 = _sut.Generate("{\"event\":\"test\"}", "secret123");
        Assert.Equal(sig1, sig2);
    }

    [Fact]
    public void Generate_DifferentSecrets_ReturnsDifferentSignatures()
    {
        var sig1 = _sut.Generate("{\"event\":\"test\"}", "secret1");
        var sig2 = _sut.Generate("{\"event\":\"test\"}", "secret2");
        Assert.NotEqual(sig1, sig2);
    }

    [Fact]
    public void Generate_DifferentPayloads_ReturnsDifferentSignatures()
    {
        var sig1 = _sut.Generate("{\"event\":\"a\"}", "secret");
        var sig2 = _sut.Generate("{\"event\":\"b\"}", "secret");
        Assert.NotEqual(sig1, sig2);
    }

    [Fact]
    public void Generate_ReturnsLowercaseHex()
    {
        var sig = _sut.Generate("payload", "secret");
        Assert.Equal(sig, sig.ToLowerInvariant());
        Assert.Equal(64, sig.Length);
    }
}
