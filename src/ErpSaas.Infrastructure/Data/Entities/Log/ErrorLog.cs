namespace ErpSaas.Infrastructure.Data.Entities.Log;

public class ErrorLog
{
    public long Id { get; set; }
    public string OperationName { get; set; } = "";
    public string ExceptionType { get; set; } = "";
    public string Message { get; set; } = "";
    public string? StackTrace { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public long? UserId { get; set; }
    public long? ShopId { get; set; }
    public string? CorrelationId { get; set; }
}
