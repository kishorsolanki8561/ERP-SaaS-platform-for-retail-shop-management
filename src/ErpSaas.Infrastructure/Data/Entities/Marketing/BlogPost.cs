using ErpSaas.Shared.Data;

namespace ErpSaas.Infrastructure.Data.Entities.Marketing;

public class BlogPost : BaseEntity
{
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string AuthorName { get; set; } = "";
    public bool IsPublished { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public string? Tags { get; set; }
}
