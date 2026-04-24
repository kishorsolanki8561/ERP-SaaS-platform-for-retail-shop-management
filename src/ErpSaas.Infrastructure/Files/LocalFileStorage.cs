using Microsoft.Extensions.Configuration;

namespace ErpSaas.Infrastructure.Files;

public sealed class LocalFileStorage(IConfiguration configuration) : IFileStorage
{
    private readonly string _root = configuration["FileStorage:LocalPath"] ?? Path.Combine(Path.GetTempPath(), "shopearth-uploads");

    public async Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName);
        var key = $"{Guid.NewGuid():N}{ext}";
        var dir = Path.Combine(_root, key[..2]);
        Directory.CreateDirectory(dir);

        var fullPath = Path.Combine(dir, key);
        await using var fs = File.OpenWrite(fullPath);
        await content.CopyToAsync(fs, ct);
        return key;
    }

    public Task<Stream> ReadAsync(string storageKey, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_root, storageKey[..2], storageKey);
        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_root, storageKey[..2], storageKey);
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public string GetPublicUrl(string storageKey) => $"/files/{storageKey}";
}
