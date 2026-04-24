using System.Net.Http.Json;
using ErpSaas.Shared.Http;

namespace ErpSaas.Infrastructure.Http;

public abstract class ThirdPartyApiClientBase : IThirdPartyApiClient
{
    protected readonly HttpClient Http;

    protected ThirdPartyApiClientBase(HttpClient httpClient)
    {
        Http = httpClient;
    }

    protected async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string url, TRequest request, CancellationToken ct)
    {
        var response = await Http.PostAsJsonAsync(url, request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(ct);
    }

    protected async Task<TResponse?> GetAsync<TResponse>(string url, CancellationToken ct)
    {
        var response = await Http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(ct);
    }
}
