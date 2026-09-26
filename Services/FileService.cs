namespace ValousWorld.Web.Services;

public class FileService : IFileService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FileService> _logger;

    private static readonly string[] AllowedExtensions =
        { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    public FileService(IWebHostEnvironment env, ILogger<FileService> logger)
    {
        _env = env;
        _logger = logger;
    }

    public async Task<string> SaveImageAsync(IFormFile file, string folder)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty.");

        if (file.Length > MaxFileSizeBytes)
            throw new InvalidOperationException("File is too large. Max 5 MB allowed.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidOperationException(
                $"Invalid file type. Allowed: {string.Join(", ", AllowedExtensions)}");

        // Build folder path: wwwroot/uploads/{folder}/{yyyy}/{MM}/
        var now = DateTime.UtcNow;
        var relativeFolder = Path.Combine("uploads", folder, now.ToString("yyyy"), now.ToString("MM"));
        var physicalFolder = Path.Combine(_env.WebRootPath, relativeFolder);

        Directory.CreateDirectory(physicalFolder);

        // Unique filename: GUID + extension
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var physicalPath = Path.Combine(physicalFolder, fileName);

        using (var stream = new FileStream(physicalPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Return URL path with forward slashes (works on all OS)
        var urlPath = "/" + relativeFolder.Replace("\\", "/") + "/" + fileName;
        _logger.LogInformation("Saved uploaded image to {Path}", urlPath);
        return urlPath;
    }

    public void DeleteImage(string? relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl)) return;

        // Skip external URLs
        if (!relativeUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            var trimmed = relativeUrl.TrimStart('/');
            var physicalPath = Path.Combine(_env.WebRootPath, trimmed.Replace("/", Path.DirectorySeparatorChar.ToString()));

            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
                _logger.LogInformation("Deleted old image {Path}", relativeUrl);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete image {Path}", relativeUrl);
        }
    }
}