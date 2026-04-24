using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;

namespace ErpSaas.Infrastructure.Files;

public sealed class AzureBlobFileStorage(IConfiguration configuration) : IFileStorage
{
    private readonly string _connectionString = configuration.GetConnectionString("AzureStorage")
        ?? throw new InvalidOperationException("Missing ConnectionStrings:AzureStorage");
    private readonly string _container = configuration["FileStorage:BlobContainer"] ?? "uploads";

    private BlobContainerClient GetContainer()
    {
        var client = new BlobContainerClient(_connectionString, _container);
        client.CreateIfNotExists(PublicAccessType.None);
        return client;
    }

    public async Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName);
        var key = $"{Guid.NewGuid():N}{ext}";
        var blob = GetContainer().GetBlobClient(key);
        await blob.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);
        return key;
    }

    public async Task<Stream> ReadAsync(string storageKey, CancellationToken ct = default)
    {
        var blob = GetContainer().GetBlobClient(storageKey);
        var response = await blob.DownloadAsync(ct);
        return response.Value.Content;
    }

    public async Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        var blob = GetContainer().GetBlobClient(storageKey);
        await blob.DeleteIfExistsAsync(cancellationToken: ct);
    }

    public string GetPublicUrl(string storageKey)
        => $"{GetContainer().Uri}/{storageKey}";
}
