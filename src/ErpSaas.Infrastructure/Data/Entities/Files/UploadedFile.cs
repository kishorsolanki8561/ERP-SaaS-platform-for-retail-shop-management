using ErpSaas.Shared.Data;

namespace ErpSaas.Infrastructure.Data.Entities.Files;

public sealed class UploadedFile : TenantEntity
{
    public string OriginalFileName { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public long? EntityId { get; set; }
    public DateTime UploadedAtUtc { get; set; }
    public long UploadedByUserId { get; set; }
}
