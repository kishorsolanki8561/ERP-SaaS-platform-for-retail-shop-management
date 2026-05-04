using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Infrastructure.Messaging;

public sealed class SendGridEmailProvider(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<SendGridEmailProvider> logger) : IEmailProvider
{
    private static readonly string ApiUrl = "https://api.sendgrid.com/v3/mail/send";

    public async Task SendAsync(string to, string subject, string body, CancellationToken ct)
    {
        var apiKey    = configuration["Notifications:SendGrid:ApiKey"];
        var fromEmail = configuration["Notifications:SendGrid:FromEmail"] ?? "noreply@shopearth.in";
        var fromName  = configuration["Notifications:SendGrid:FromName"]  ?? "ShopEarth ERP";

        if (string.IsNullOrEmpty(apiKey))
        {
            logger.LogWarning("SendGrid API key not configured — email to {To} skipped", to);
            return;
        }

        var payload = new
        {
            personalizations = new[] { new { to = new[] { new { email = to } } } },
            from    = new { email = fromEmail, name = fromName },
            subject = subject,
            content = new[] { new { type = "text/html", value = body } },
        };

        var client = httpClientFactory.CreateClient("sendgrid");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(ApiUrl, content, ct);
        response.EnsureSuccessStatusCode();

        logger.LogInformation("Email sent via SendGrid to {To} subject '{Subject}'", to, subject);
    }
}
