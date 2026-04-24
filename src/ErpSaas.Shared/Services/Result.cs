using System.Net;

namespace ErpSaas.Shared.Services;

public class Result
{
    public bool IsSuccess { get; protected init; }
    public HttpStatusCode StatusCode { get; protected init; }
    public IReadOnlyList<string> Errors { get; protected init; } = [];

    public static Result Success() =>
        new() { IsSuccess = true, StatusCode = HttpStatusCode.OK };

    public static Result NotFound(string message) =>
        new() { IsSuccess = false, StatusCode = HttpStatusCode.NotFound, Errors = [message] };

    public static Result Conflict(string message) =>
        new() { IsSuccess = false, StatusCode = HttpStatusCode.Conflict, Errors = [message] };

    public static Result Validation(IEnumerable<string> errors) =>
        new() { IsSuccess = false, StatusCode = HttpStatusCode.UnprocessableEntity, Errors = [..errors] };

    public static Result Forbidden(string? message = null) =>
        new() { IsSuccess = false, StatusCode = HttpStatusCode.Forbidden, Errors = [message ?? "Access denied."] };

    public static Result Cancelled() =>
        new() { IsSuccess = false, StatusCode = HttpStatusCode.Gone, Errors = ["Operation was cancelled."] };

    public static Result Failure(string message) =>
        new() { IsSuccess = false, StatusCode = HttpStatusCode.InternalServerError, Errors = [message] };
}

public sealed class Result<T> : Result
{
    public T? Value { get; private init; }

    public static Result<T> Success(T value) =>
        new() { IsSuccess = true, StatusCode = HttpStatusCode.OK, Value = value };

    public new static Result<T> NotFound(string message) =>
        new() { IsSuccess = false, StatusCode = HttpStatusCode.NotFound, Errors = [message] };

    public new static Result<T> Conflict(string message) =>
        new() { IsSuccess = false, StatusCode = HttpStatusCode.Conflict, Errors = [message] };

    public new static Result<T> Validation(IEnumerable<string> errors) =>
        new() { IsSuccess = false, StatusCode = HttpStatusCode.UnprocessableEntity, Errors = [..errors] };

    public new static Result<T> Forbidden(string? message = null) =>
        new() { IsSuccess = false, StatusCode = HttpStatusCode.Forbidden, Errors = [message ?? "Access denied."] };

    public new static Result<T> Cancelled() =>
        new() { IsSuccess = false, StatusCode = HttpStatusCode.Gone, Errors = ["Operation was cancelled."] };

    public new static Result<T> Failure(string message) =>
        new() { IsSuccess = false, StatusCode = HttpStatusCode.InternalServerError, Errors = [message] };
}
