using ErpSaas.Modules.Identity.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Identity.Controllers;

[Route("api/marketing")]
public sealed class MarketingController(IMarketingContentService contentService) : BaseController
{
    // ── Content ───────────────────────────────────────────────────────────────

    [HttpGet("content")]
    [AllowAnonymous]
    public async Task<IActionResult> GetContent(
        [FromQuery] string key,
        [FromQuery] string locale = "en",
        CancellationToken ct = default)
    {
        var content = await contentService.GetAsync(key, locale, ct);
        return content is null ? NotFound() : Ok(content);
    }

    [HttpPatch("content/{key}")]
    [Authorize]
    [RequirePermission("Marketing.Edit")]
    public async Task<IActionResult> UpsertContent(
        string key,
        [FromBody] UpsertContentDto dto,
        [FromQuery] string locale = "en",
        CancellationToken ct = default)
    {
        var result = await contentService.UpsertAsync(key, locale, dto, ct);
        return Ok(result);
    }

    // ── Blog ──────────────────────────────────────────────────────────────────

    [HttpGet("blog")]
    [AllowAnonymous]
    public async Task<IActionResult> ListBlog(
        [FromQuery] bool publishedOnly = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var (items, total) = await contentService.ListBlogAsync(page, pageSize, publishedOnly, ct);
        return Ok(new { items, totalCount = total, page, pageSize });
    }

    [HttpGet("blog/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBlog(string slug, CancellationToken ct)
    {
        var post = await contentService.GetBlogAsync(slug, ct);
        return post is null ? NotFound() : Ok(post);
    }

    [HttpPost("blog")]
    [Authorize]
    [RequirePermission("Blog.Edit")]
    public async Task<IActionResult> CreateBlog([FromBody] CreateBlogPostDto dto, CancellationToken ct)
    {
        var result = await contentService.CreateBlogAsync(dto, ct);
        return Ok(result);
    }

    [HttpPatch("blog/{slug}")]
    [Authorize]
    [RequirePermission("Blog.Edit")]
    public async Task<IActionResult> UpdateBlog(string slug, [FromBody] UpdateBlogPostDto dto, CancellationToken ct)
    {
        var result = await contentService.UpdateBlogAsync(slug, dto, ct);
        return Ok(result);
    }

    [HttpPost("blog/{slug}/publish")]
    [Authorize]
    [RequirePermission("Blog.Publish")]
    public async Task<IActionResult> PublishBlog(string slug, CancellationToken ct)
    {
        var result = await contentService.PublishBlogAsync(slug, ct);
        return Ok(result);
    }
}
