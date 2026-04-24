namespace ErpSaas.Infrastructure.Files;

public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct = default);
    Task<Stream> ReadAsync(string storageKey, CancellationToken ct = default);
    Task DeleteAsync(string storageKey, CancellationToken ct = default);
    string GetPublicUrl(string storageKey);
}
