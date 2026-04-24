namespace ErpSaas.Infrastructure.Ddl;

public record DdlItemDto(string Code, string Label, int SortOrder, string? ParentCode);

public interface IDdlService
{
    Task<IReadOnlyList<DdlItemDto>> GetItemsAsync(
        string key,
        long shopId,
        string? parentCode,
        CancellationToken ct);

    Task<IReadOnlyDictionary<string, IReadOnlyList<DdlItemDto>>> GetBatchAsync(
        IEnumerable<string> keys,
        long shopId,
        CancellationToken ct);
}
