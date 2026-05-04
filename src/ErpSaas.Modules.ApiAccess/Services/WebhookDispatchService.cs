using System.Text.Json;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.ApiAccess.Entities;
using ErpSaas.Modules.ApiAccess.Enums;
using ErpSaas.Modules.ApiAccess.Jobs;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.ApiAccess.Services;

public sealed class WebhookDispatchService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    IBackgroundJobClient jobClient,
    ITenantContext tenant,
    ILogger<WebhookDispatchService> logger)
    : BaseService<TenantDbContext>(db, errorLogger), IWebhookDispatchService
{
    private static readonly IReadOnlyList<string> _eventCatalog =
    [
        "invoice.created", "invoice.finalized", "invoice.cancelled", "invoice.refunded",
        "payment.received", "payment.failed", "wallet.topped_up", "wallet.debited",
        "quotation.sent", "quotation.accepted", "quotation.converted",
        "stock.low", "stock.adjusted", "product.created", "product.updated",
        "customer.created", "customer.updated",
        "shift.opened", "shift.closed", "shift.variance_detected",
        "purchase_order.created", "purchase_order.received",
        "warranty.registered", "warranty.claim_created",
        "subscription.renewed", "subscription.cancelled",
    ];

    public IReadOnlyList<string> GetEventCatalog() => _eventCatalog;

    public async Task DispatchAsync(long shopId, string eventCode, object payload, CancellationToken ct = default)
    {
        var payloadJson = JsonSerializer.Serialize(payload);

        var endpoints = await _db.Set<WebhookEndpoint>()
            .IgnoreQueryFilters()
            .Where(e => e.ShopId == shopId && e.IsActive)
            .ToListAsync(ct);

        foreach (var ep in endpoints)
        {
            var subscribedEvents = ep.EventsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!subscribedEvents.Contains(eventCode)) continue;

            var delivery = new WebhookDelivery
            {
                ShopId = shopId,
                WebhookEndpointId = ep.Id,
                EventCode = eventCode,
                DeliveryId = Guid.NewGuid(),
                PayloadJson = payloadJson,
                AttemptNumber = 0,
                Status = WebhookDeliveryStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow,
            };
            _db.Set<WebhookDelivery>().Add(delivery);
            await _db.SaveChangesAsync(ct);

            jobClient.Enqueue<WebhookDeliveryJob>(j => j.ExecuteAsync(delivery.Id, CancellationToken.None));
            logger.LogInformation("Queued webhook delivery {DeliveryId} for event {Event} to endpoint {EndpointId}",
                delivery.DeliveryId, eventCode, ep.Id);
        }
    }

    public async Task<Result<EndpointListItem>> RegisterEndpointAsync(RegisterEndpointDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("ApiAccess.RegisterEndpoint", async () =>
        {
            if (!Uri.TryCreate(dto.Url, UriKind.Absolute, out var uri) || uri.Scheme != "https")
                return Result<EndpointListItem>.Failure(Errors.ApiAccess.InvalidWebhookUrl);

            var secret = GenerateSecret();
            var ep = new WebhookEndpoint
            {
                ShopId = tenant.ShopId,
                Name = dto.Name,
                Url = dto.Url,
                SigningSecret = secret,
                EventsCsv = dto.EventsCsv,
                IsActive = true,
                MaxRetries = dto.MaxRetries,
                TimeoutSeconds = dto.TimeoutSeconds,
                CustomHeadersJson = dto.CustomHeadersJson,
                CreatedAtUtc = DateTime.UtcNow,
            };
            _db.Set<WebhookEndpoint>().Add(ep);
            await _db.SaveChangesAsync(ct);

            return Result<EndpointListItem>.Success(ToListItem(ep));
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> UpdateEndpointAsync(long id, UpdateEndpointDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("ApiAccess.UpdateEndpoint", async () =>
        {
            var ep = await _db.Set<WebhookEndpoint>().FirstOrDefaultAsync(e => e.Id == id, ct);
            if (ep is null) return Result<bool>.NotFound(Errors.ApiAccess.EndpointNotFound);

            if (!Uri.TryCreate(dto.Url, UriKind.Absolute, out var uri) || uri.Scheme != "https")
                return Result<bool>.Failure(Errors.ApiAccess.InvalidWebhookUrl);

            ep.Name = dto.Name;
            ep.Url = dto.Url;
            ep.EventsCsv = dto.EventsCsv;
            ep.IsActive = dto.IsActive;
            ep.MaxRetries = dto.MaxRetries;
            ep.TimeoutSeconds = dto.TimeoutSeconds;
            ep.CustomHeadersJson = dto.CustomHeadersJson;
            await _db.SaveChangesAsync(ct);

            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<Result<string>> RotateSecretAsync(long id, CancellationToken ct = default)
    {
        return await ExecuteAsync("ApiAccess.RotateSecret", async () =>
        {
            var ep = await _db.Set<WebhookEndpoint>().FirstOrDefaultAsync(e => e.Id == id, ct);
            if (ep is null) return Result<string>.NotFound(Errors.ApiAccess.EndpointNotFound);

            ep.SigningSecret = GenerateSecret();
            await _db.SaveChangesAsync(ct);

            return Result<string>.Success(ep.SigningSecret);
        }, ct, useTransaction: true);
    }

    public async Task<Result<IReadOnlyList<EndpointListItem>>> ListEndpointsAsync(CancellationToken ct = default)
    {
        return await ExecuteAsync("ApiAccess.ListEndpoints", async () =>
        {
            var list = await _db.Set<WebhookEndpoint>()
                .OrderByDescending(e => e.CreatedAtUtc)
                .ToListAsync(ct);
            return Result<IReadOnlyList<EndpointListItem>>.Success(list.Select(ToListItem).ToList());
        }, ct);
    }

    public async Task<Result<IReadOnlyList<DeliveryListItem>>> ListDeliveriesAsync(int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        return await ExecuteAsync("ApiAccess.ListDeliveries", async () =>
        {
            var list = await _db.Set<WebhookDelivery>()
                .OrderByDescending(d => d.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new DeliveryListItem(
                    d.Id, d.WebhookEndpointId, d.EventCode, d.DeliveryId,
                    d.AttemptNumber, d.ResponseStatusCode, d.Status.ToString(),
                    d.ErrorMessage, d.DeliveredAtUtc, d.CreatedAtUtc))
                .ToListAsync(ct);
            return Result<IReadOnlyList<DeliveryListItem>>.Success(list);
        }, ct);
    }

    public async Task<Result<bool>> RetryDeliveryAsync(long deliveryId, CancellationToken ct = default)
    {
        return await ExecuteAsync("ApiAccess.RetryDelivery", async () =>
        {
            var delivery = await _db.Set<WebhookDelivery>().FirstOrDefaultAsync(d => d.Id == deliveryId, ct);
            if (delivery is null) return Result<bool>.NotFound(Errors.ApiAccess.DeliveryNotFound);
            if (delivery.Status == WebhookDeliveryStatus.Succeeded)
                return Result<bool>.Conflict(Errors.ApiAccess.DeliveryAlreadySucceeded);

            delivery.Status = WebhookDeliveryStatus.Pending;
            delivery.NextRetryAtUtc = null;
            await _db.SaveChangesAsync(ct);

            jobClient.Enqueue<WebhookDeliveryJob>(j => j.ExecuteAsync(deliveryId, CancellationToken.None));
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    private static string GenerateSecret()
    {
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        return "whsec_" + Convert.ToBase64String(bytes).Replace("+", "A").Replace("/", "B").Replace("=", "");
    }

    private static EndpointListItem ToListItem(WebhookEndpoint ep) =>
        new(ep.Id, ep.Name, ep.Url, ep.EventsCsv, ep.IsActive, ep.MaxRetries, ep.TimeoutSeconds, ep.CreatedAtUtc);
}
