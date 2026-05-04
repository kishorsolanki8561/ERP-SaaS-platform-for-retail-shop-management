using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Marketing;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Identity.Services;

public sealed class MarketingContentService(
    PlatformDbContext db,
    IErrorLogger errorLogger,
    ILogger<MarketingContentService> logger)
    : BaseService<PlatformDbContext>(db, errorLogger), IMarketingContentService
{
    public async Task<MarketingContentDto?> GetAsync(string key, string locale, CancellationToken ct = default)
    {
        var content = await db.MarketingContents.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Key == key && c.Locale == locale, ct);

        if (content is null) return null;

        return new MarketingContentDto(
            content.Id, content.Key, content.Locale,
            content.Title, content.Body, content.UpdatedAtUtc ?? content.CreatedAtUtc);
    }

    public async Task<Result<bool>> UpsertAsync(string key, string locale, UpsertContentDto dto, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Marketing.UpsertContent", async () =>
        {
            var existing = await db.MarketingContents
                .FirstOrDefaultAsync(c => c.Key == key && c.Locale == locale, ct);

            if (existing is null)
            {
                db.MarketingContents.Add(new MarketingContent
                {
                    Key          = key,
                    Locale       = locale,
                    Title        = dto.Title,
                    Body         = dto.Body,
                    CreatedAtUtc = DateTime.UtcNow,
                });
            }
            else
            {
                existing.Title        = dto.Title;
                existing.Body         = dto.Body;
                existing.UpdatedAtUtc = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(ct);

            logger.LogInformation("Marketing content upserted: {Key}/{Locale}", key, locale);

            return Result<bool>.Success(true);
        }, ct);

    public async Task<(IReadOnlyList<BlogPostSummaryDto> Items, int TotalCount)> ListBlogAsync(
        int page, int pageSize, bool publishedOnly, CancellationToken ct = default)
    {
        var query = db.BlogPosts.AsNoTracking();

        if (publishedOnly)
            query = query.Where(p => p.IsPublished);

        var total = await query.CountAsync(ct);

        var posts = await query
            .OrderByDescending(p => p.PublishedAtUtc ?? p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new BlogPostSummaryDto(
                p.Id, p.Slug, p.Title, p.AuthorName,
                p.IsPublished, p.PublishedAtUtc, p.Tags, p.CreatedAtUtc))
            .ToListAsync(ct);

        return (posts, total);
    }

    public async Task<BlogPostDetailDto?> GetBlogAsync(string slug, CancellationToken ct = default)
    {
        var post = await db.BlogPosts.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == slug, ct);

        if (post is null) return null;

        return new BlogPostDetailDto(
            post.Id, post.Slug, post.Title, post.Body,
            post.AuthorName, post.IsPublished, post.PublishedAtUtc,
            post.Tags, post.CreatedAtUtc);
    }

    public async Task<Result<long>> CreateBlogAsync(CreateBlogPostDto dto, CancellationToken ct = default)
        => await ExecuteAsync<long>("Blog.Create", async () =>
        {
            var slugExists = await db.BlogPosts.AnyAsync(p => p.Slug == dto.Slug, ct);
            if (slugExists) return Result<long>.Conflict(Errors.Lead.SlugExists);

            var post = new BlogPost
            {
                Slug         = dto.Slug,
                Title        = dto.Title,
                Body         = dto.Body,
                AuthorName   = dto.AuthorName,
                Tags         = dto.Tags,
                IsPublished  = false,
                CreatedAtUtc = DateTime.UtcNow,
            };

            db.BlogPosts.Add(post);
            await db.SaveChangesAsync(ct);

            return Result<long>.Success(post.Id);
        }, ct);

    public async Task<Result<bool>> UpdateBlogAsync(string slug, UpdateBlogPostDto dto, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Blog.Update", async () =>
        {
            var post = await db.BlogPosts.FirstOrDefaultAsync(p => p.Slug == slug, ct);
            if (post is null) return Result<bool>.NotFound(Errors.Lead.BlogPostNotFound);

            if (dto.Title is not null) post.Title      = dto.Title;
            if (dto.Body is not null)  post.Body       = dto.Body;
            if (dto.AuthorName is not null) post.AuthorName = dto.AuthorName;
            if (dto.Tags is not null)  post.Tags       = dto.Tags;
            post.UpdatedAtUtc = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);

            return Result<bool>.Success(true);
        }, ct);

    public async Task<Result<bool>> PublishBlogAsync(string slug, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Blog.Publish", async () =>
        {
            var post = await db.BlogPosts.FirstOrDefaultAsync(p => p.Slug == slug, ct);
            if (post is null) return Result<bool>.NotFound(Errors.Lead.BlogPostNotFound);

            if (post.IsPublished) return Result<bool>.Conflict(Errors.Lead.BlogPostAlreadyPublished);

            post.IsPublished      = true;
            post.PublishedAtUtc   = DateTime.UtcNow;
            post.UpdatedAtUtc     = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);

            logger.LogInformation("Blog post published: {Slug}", slug);

            return Result<bool>.Success(true);
        }, ct);
}
