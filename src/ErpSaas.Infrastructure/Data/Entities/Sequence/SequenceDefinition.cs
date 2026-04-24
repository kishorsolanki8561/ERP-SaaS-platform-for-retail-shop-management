using ErpSaas.Shared.Data;

namespace ErpSaas.Infrastructure.Data.Entities.Sequence;

public class SequenceDefinition : TenantEntity
{
    public string Code { get; set; } = "";
    public string? Prefix { get; set; }
    public string? Suffix { get; set; }
    public long LastNumber { get; set; }
    public int PadLength { get; set; } = 6;
}
