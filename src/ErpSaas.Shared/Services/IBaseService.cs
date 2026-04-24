namespace ErpSaas.Shared.Services;

public interface IBaseService
{
    Task<Result<T>> ExecuteAsync<T>(
        string operationName,
        Func<Task<Result<T>>> operation,
        CancellationToken ct,
        bool useTransaction = false);
}
