using System.Text.Json;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Modules.ApiAccess.Entities;
using ErpSaas.Modules.ApiAccess.Enums;
using ErpSaas.Modules.ApiAccess.Services;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.ApiAccess.Jobs;

public sealed class WebhookDeliveryJob(
    TenantDbContext db,
    IWebhookSignatureGenerator signer,
    IHttpClientFactory httpClientFactory,
    IBackgroundJobClient jobClient,
    ILogger<WebhookDeliveryJob> logger)
{
    private static readonly int[] BackoffSeconds = [1, 4, 16, 60, 300];

    public async Task ExecuteAsync(long deliveryId, CancellationToken ct = default)
    {
        var delivery = await db.Set<WebhookDelivery>()
            .IgnoreQueryFilters()
            .Include(d => d.Endpoint)
            .FirstOrDefaultAsync(d => d.Id == deliveryId, ct);

        if (delivery is null)
        {
            logger.LogWarning("WebhookDeliveryJob: delivery {Id} not found", deliveryId);
            return;
        }

        delivery.AttemptNumber++;
        var ep = delivery.Endpoint;

        using var client = httpClientFactory.CreateClient("WebhookClient");
        client.Timeout = TimeSpan.FromSeconds(ep.TimeoutSeconds);

        var signature = signer.Generate(delivery.PayloadJson, ep.SigningSecret);

        using var request = new HttpRequestMessage(HttpMethod.Post, ep.Url);
        request.Content = new StringContent(delivery.PayloadJson, System.Text.Encoding.UTF8, "application/json");
        request.Headers.TryAddWithoutValidation("X-ShopSphere-Signature", signature);
        request.Headers.TryAddWithoutValidation("X-ShopSphere-Delivery", delivery.DeliveryId.ToString());
        request.Headers.TryAddWithoutValidation("X-ShopSphere-Event", delivery.EventCode);

        if (!string.IsNullOrEmpty(ep.CustomHeadersJson))
        {
            var customHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(ep.CustomHeadersJson);
            if (customHeaders is not null)
                foreach (var (k, v) in customHeaders)
                    request.Headers.TryAddWithoutValidation(k, v);
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            using var response = await client.SendAsync(request, ct);
            sw.Stop();

            delivery.ResponseStatusCode = (int)response.StatusCode;
            delivery.ResponseTimeMs = (int)sw.ElapsedMilliseconds;
            var body = await response.Content.ReadAsStringAsync(ct);
            delivery.ResponseBody = body.Length > 4000 ? body[..4000] : body;

            if (response.IsSuccessStatusCode)
            {
                delivery.Status = WebhookDeliveryStatus.Succeeded;
                delivery.DeliveredAtUtc = DateTime.UtcNow;
                logger.LogInformation("Webhook delivery {Id} succeeded with {Status}", deliveryId, (int)response.StatusCode);
            }
            else
            {
                HandleFailure(delivery, ep, $"HTTP {(int)response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            delivery.ResponseTimeMs = (int)sw.ElapsedMilliseconds;
            delivery.ErrorMessage = ex.Message[..Math.Min(ex.Message.Length, 2000)];
            HandleFailure(delivery, ep, ex.Message);
            logger.LogWarning(ex, "Webhook delivery {Id} attempt {Attempt} threw exception", deliveryId, delivery.AttemptNumber);
        }

        await db.SaveChangesAsync(ct);

        if (delivery.Status == WebhookDeliveryStatus.Pending && delivery.NextRetryAtUtc.HasValue)
        {
            var delay = delivery.NextRetryAtUtc.Value - DateTime.UtcNow;
            jobClient.Schedule<WebhookDeliveryJob>(
                j => j.ExecuteAsync(deliveryId, CancellationToken.None),
                delay > TimeSpan.Zero ? delay : TimeSpan.Zero);
        }
    }

    private static void HandleFailure(WebhookDelivery delivery, WebhookEndpoint ep, string reason)
    {
        var attemptIndex = delivery.AttemptNumber - 1;
        if (attemptIndex < ep.MaxRetries && attemptIndex < BackoffSeconds.Length)
        {
            delivery.Status = WebhookDeliveryStatus.Pending;
            delivery.NextRetryAtUtc = DateTime.UtcNow.AddSeconds(BackoffSeconds[attemptIndex]);
        }
        else
        {
            delivery.Status = WebhookDeliveryStatus.DeadLettered;
            delivery.ErrorMessage ??= reason;
        }
    }
}
