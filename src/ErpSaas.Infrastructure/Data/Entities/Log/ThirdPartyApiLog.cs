namespace ErpSaas.Infrastructure.Data.Entities.Log;

public class ThirdPartyApiLog
{
    public long Id { get; set; }
    public string Provider { get; set; } = "";
    public string HttpMethod { get; set; } = "";
    public string Url { get; set; } = "";
    public string? RequestBody { get; set; }
    public int? ResponseStatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public int DurationMs { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public long? UserId { get; set; }
    public long? ShopId { get; set; }
    public DateTime CalledAtUtc { get; set; }
}
