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
        // Write to Serilog immediately so the error is always visible in the API console.
        _logger.LogError(ex, "Operation '{Operation}' threw {ExceptionType}: {Message}",
            operationName, ex.GetType().Name, ex.Message);

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
        _channel.Writer.TryComplete();
        if (_cts is not null)
        {
            try { await _cts.CancelAsync(); }
            catch (ObjectDisposedException) { } // linked CTS disposed by host before StopAsync fires
        }
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
            try { await _cts.CancelAsync(); }
            catch (ObjectDisposedException) { } // may already be disposed by host shutdown
            try { _cts.Dispose(); }
            catch (ObjectDisposedException) { }
        }
    }
}
