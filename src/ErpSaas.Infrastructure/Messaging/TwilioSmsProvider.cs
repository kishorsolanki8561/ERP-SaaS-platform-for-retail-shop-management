using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Infrastructure.Messaging;

public sealed class TwilioSmsProvider(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<TwilioSmsProvider> logger) : ISmsProvider
{
    public async Task SendAsync(string to, string body, CancellationToken ct)
    {
        var accountSid = configuration["Notifications:Twilio:AccountSid"];
        var authToken  = configuration["Notifications:Twilio:AuthToken"];
        var fromNumber = configuration["Notifications:Twilio:FromNumber"];

        if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken))
        {
            logger.LogWarning("Twilio credentials not configured — SMS to {To} skipped", to);
            return;
        }

        var url = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";

        var credentials = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));

        var client = httpClientFactory.CreateClient("twilio");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);

        var formData = new Dictionary<string, string>
        {
            ["To"]   = to,
            ["From"] = fromNumber ?? "",
            ["Body"] = body,
        };

        using var content = new FormUrlEncodedContent(formData);
        var response = await client.PostAsync(url, content, ct);
        response.EnsureSuccessStatusCode();

        logger.LogInformation("SMS sent via Twilio to {To}", to);
    }
}
