using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Identity.Services;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public sealed record MarketingContentDto(
    long Id,
    string Key,
    string Locale,
    string? Title,
    string Body,
    DateTime UpdatedAtUtc);

public sealed record UpsertContentDto(
    string? Title,
    string Body);

public sealed record BlogPostSummaryDto(
    long Id,
    string Slug,
    string Title,
    string AuthorName,
    bool IsPublished,
    DateTime? PublishedAtUtc,
    string? Tags,
    DateTime CreatedAtUtc);

public sealed record BlogPostDetailDto(
    long Id,
    string Slug,
    string Title,
    string Body,
    string AuthorName,
    bool IsPublished,
    DateTime? PublishedAtUtc,
    string? Tags,
    DateTime CreatedAtUtc);

public sealed record CreateBlogPostDto(
    string Slug,
    string Title,
    string Body,
    string AuthorName,
    string? Tags);

public sealed record UpdateBlogPostDto(
    string? Title,
    string? Body,
    string? AuthorName,
    string? Tags);

// ── Interface ────────────────────────────────────────────────────────────────

public interface IMarketingContentService
{
    Task<MarketingContentDto?>  GetAsync(string key, string locale, CancellationToken ct = default);
    Task<Result<bool>>          UpsertAsync(string key, string locale, UpsertContentDto dto, CancellationToken ct = default);

    Task<(IReadOnlyList<BlogPostSummaryDto> Items, int TotalCount)> ListBlogAsync(int page, int pageSize, bool publishedOnly, CancellationToken ct = default);
    Task<BlogPostDetailDto?>    GetBlogAsync(string slug, CancellationToken ct = default);
    Task<Result<long>>          CreateBlogAsync(CreateBlogPostDto dto, CancellationToken ct = default);
    Task<Result<bool>>          UpdateBlogAsync(string slug, UpdateBlogPostDto dto, CancellationToken ct = default);
    Task<Result<bool>>          PublishBlogAsync(string slug, CancellationToken ct = default);
}
