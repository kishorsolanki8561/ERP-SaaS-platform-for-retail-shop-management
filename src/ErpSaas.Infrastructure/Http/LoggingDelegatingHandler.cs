using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Log;

namespace ErpSaas.Infrastructure.Http;

public sealed class LoggingDelegatingHandler(LogDbContext db) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken ct)
    {
        var started = DateTime.UtcNow;
        string? requestBody = null;
        string? responseBody = null;
        int? statusCode = null;
        bool isSuccess = false;
        string? errorMessage = null;

        if (request.Content is not null)
            requestBody = await request.Content.ReadAsStringAsync(ct);

        try
        {
            var response = await base.SendAsync(request, ct);
            statusCode = (int)response.StatusCode;
            isSuccess = response.IsSuccessStatusCode;
            responseBody = await response.Content.ReadAsStringAsync(ct);
            return response;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            throw;
        }
        finally
        {
            var durationMs = (int)(DateTime.UtcNow - started).TotalMilliseconds;

            var log = new ThirdPartyApiLog
            {
                Provider = request.Headers.Host ?? request.RequestUri?.Host ?? "unknown",
                HttpMethod = request.Method.Method,
                Url = request.RequestUri?.ToString() ?? "",
                RequestBody = requestBody,
                ResponseStatusCode = statusCode,
                ResponseBody = responseBody,
                DurationMs = durationMs,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                CalledAtUtc = started
            };

            try
            {
                db.ThirdPartyApiLogs.Add(log);
                await db.SaveChangesAsync(CancellationToken.None);
            }
            catch
            {
                // log persistence failure must not propagate
            }
        }
    }
}
