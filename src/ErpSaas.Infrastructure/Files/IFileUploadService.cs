using ErpSaas.Shared.Services;

namespace ErpSaas.Infrastructure.Files;

public record UploadFileRequest(
    Stream Content,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string Purpose,
    string? EntityType = null,
    long? EntityId = null);

public record UploadFileResponse(
    long Id,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string Purpose,
    string? EntityType,
    long? EntityId,
    string Url,
    DateTime UploadedAtUtc);

public interface IFileUploadService
{
    Task<Result<UploadFileResponse>> UploadAsync(UploadFileRequest request, CancellationToken ct = default);
    Task<UploadFileResponse?> GetAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<UploadFileResponse>> ListByEntityAsync(string entityType, long entityId, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(long id, CancellationToken ct = default);
}
