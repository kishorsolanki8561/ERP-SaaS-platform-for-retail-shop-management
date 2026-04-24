namespace ErpSaas.Infrastructure.Data.Entities.Log;

public class SequenceAllocation
{
    public long Id { get; set; }
    public long ShopId { get; set; }
    public string Code { get; set; } = "";
    public long AllocatedNumber { get; set; }
    public DateTime AllocatedAtUtc { get; set; }
}
