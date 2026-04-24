using ErpSaas.Infrastructure.Services;
using ErpSaas.Shared.Services;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ErpSaas.Tests.Unit;

[Trait("Category", "Unit")]
public class BaseServiceTests
{
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly DbContext _db = Substitute.For<DbContext>();

    private BaseService<DbContext> CreateSut() =>
        new(_db, _errorLogger);

    [Fact]
    public async Task ExecuteAsync_HappyPath_ReturnsSuccess()
    {
        var sut = CreateSut();

        var result = await sut.ExecuteAsync(
            "Test.Operation",
            () => Task.FromResult(Result<int>.Success(42)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task ExecuteAsync_OperationCanceled_ReturnsCancelled()
    {
        var sut = CreateSut();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await sut.ExecuteAsync<int>(
            "Test.Operation",
            () => throw new OperationCanceledException(cts.Token),
            cts.Token);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Gone);
    }

    [Fact]
    public async Task ExecuteAsync_DbUpdateConcurrencyException_ReturnsConflict()
    {
        var sut = CreateSut();

        var result = await sut.ExecuteAsync<int>(
            "Test.Operation",
            () => throw new DbUpdateConcurrencyException("row version mismatch"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ExecuteAsync_ValidationException_ReturnsValidation()
    {
        var sut = CreateSut();
        var failures = new[] { new ValidationFailure("Name", "Name is required.") };

        var result = await sut.ExecuteAsync<int>(
            "Test.Operation",
            () => throw new ValidationException(failures),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.UnprocessableEntity);
        result.Errors.Should().ContainSingle(e => e == "Name is required.");
    }

    [Fact]
    public async Task ExecuteAsync_UnhandledException_LogsAndReturnsFailure()
    {
        var sut = CreateSut();
        var boom = new InvalidOperationException("something exploded");

        var result = await sut.ExecuteAsync<int>(
            "Test.Operation",
            () => throw boom,
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.InternalServerError);
        await _errorLogger.Received(1)
            .LogAsync("Test.Operation", boom, Arg.Any<CancellationToken>());
    }
}
