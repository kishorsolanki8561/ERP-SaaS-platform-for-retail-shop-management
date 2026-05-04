using System.Security.Cryptography;
using System.Text;

namespace ErpSaas.Modules.ApiAccess.Services;

public sealed class WebhookSignatureGenerator : IWebhookSignatureGenerator
{
    public string Generate(string payloadJson, string signingSecret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(signingSecret);
        var msgBytes = Encoding.UTF8.GetBytes(payloadJson);
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(msgBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
