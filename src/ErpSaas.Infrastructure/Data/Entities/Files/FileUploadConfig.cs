namespace ErpSaas.Infrastructure.Data.Entities.Files;

public sealed class FileUploadConfig
{
    public long Id { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public long MaxSizeBytes { get; set; } = 5_242_880; // 5 MB default
    public string AllowedExtensions { get; set; } = ".jpg,.jpeg,.png,.pdf";
    public bool IsActive { get; set; } = true;
}
