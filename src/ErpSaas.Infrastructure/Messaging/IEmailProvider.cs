namespace ErpSaas.Infrastructure.Messaging;

public interface IEmailProvider
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct);
}
