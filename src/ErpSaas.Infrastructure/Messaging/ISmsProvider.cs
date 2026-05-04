namespace ErpSaas.Infrastructure.Messaging;

public interface ISmsProvider
{
    Task SendAsync(string to, string body, CancellationToken ct);
}
