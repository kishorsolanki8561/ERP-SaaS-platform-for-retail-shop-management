using System.Threading.Channels;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Log;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Infrastructure.Services;

public sealed class ErrorLogger : IErrorLogger, IHostedService, IAsyncDisposable
{
    private readonly Channel<ErrorLog> _channel = Channel.CreateUnbounded<ErrorLog>(
        new UnboundedChannelOptions { SingleReader = true });
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ErrorLogger> _logger;
    private Task? _drainTask;
    private CancellationTokenSource? _cts;

    public ErrorLogger(IServiceScopeFactory scopeFactory, ILogger<ErrorLogger> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task LogAsync(string operationName, Exception ex, CancellationToken ct = default)
    {
        var entry = new ErrorLog
        {
            OperationName = operationName,
            ExceptionType = ex.GetType().FullName ?? ex.GetType().Name,
            Message = ex.Message,
            StackTrace = ex.StackTrace,
            OccurredAtUtc = DateTime.UtcNow
        };

        _channel.Writer.TryWrite(entry);
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _drainTask = DrainAsync(_cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken ct)
    {
        _channel.Writer.TryComplete(); // TryComplete is idempotent; Complete() throws if already done
        if (_cts is not null) await _cts.CancelAsync();
        if (_drainTask is not null)
        {
            try { await _drainTask; }
            catch (OperationCanceledException) { }
        }
    }

    private async Task DrainAsync(CancellationToken ct)
    {
        await foreach (var entry in _channel.Reader.ReadAllAsync(ct))
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<LogDbContext>();
                db.ErrorLogs.Add(entry);
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist ErrorLog for operation {Op}", entry.OperationName);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();
            _cts.Dispose();
        }
    }
}
