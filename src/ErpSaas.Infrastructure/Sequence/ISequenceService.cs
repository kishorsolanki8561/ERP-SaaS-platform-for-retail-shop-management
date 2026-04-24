namespace ErpSaas.Infrastructure.Sequence;

public interface ISequenceService
{
    Task<string> NextAsync(string code, long shopId, CancellationToken ct);
}
