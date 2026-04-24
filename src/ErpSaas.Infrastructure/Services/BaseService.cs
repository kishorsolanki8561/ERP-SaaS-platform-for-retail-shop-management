using ErpSaas.Shared.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ErpSaas.Infrastructure.Services;

public class BaseService<TDbContext>(
    TDbContext db,
    IErrorLogger errorLogger)
    : IBaseService
    where TDbContext : DbContext
{
    public async Task<Result<T>> ExecuteAsync<T>(
        string operationName,
        Func<Task<Result<T>>> operation,
        CancellationToken ct,
        bool useTransaction = false)
    {
        IDbContextTransaction? tx = null;
        try
        {
            if (useTransaction)
                tx = await db.Database.BeginTransactionAsync(ct);

            var result = await operation();

            if (useTransaction && tx is not null)
            {
                if (result.IsSuccess)
                    await tx.CommitAsync(ct);
                else
                    await tx.RollbackAsync(ct);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            if (tx is not null) await RollbackSilentlyAsync(tx);
            return Result<T>.Cancelled();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            if (tx is not null) await RollbackSilentlyAsync(tx);
            return Result<T>.Conflict($"Concurrency conflict in '{operationName}': {ex.Message}");
        }
        catch (ValidationException ex)
        {
            if (tx is not null) await RollbackSilentlyAsync(tx);
            return Result<T>.Validation(ex.Errors.Select(e => e.ErrorMessage));
        }
        catch (Exception ex)
        {
            if (tx is not null) await RollbackSilentlyAsync(tx);
            await errorLogger.LogAsync(operationName, ex);
            return Result<T>.Failure($"An unexpected error occurred in '{operationName}'.");
        }
        finally
        {
            if (tx is not null)
                await tx.DisposeAsync();
        }
    }

    private static async Task RollbackSilentlyAsync(IDbContextTransaction tx)
    {
        try { await tx.RollbackAsync(); }
        catch { /* rollback best-effort */ }
    }
}
