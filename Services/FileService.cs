namespace ValousWorld.Web.Services;

public class FileService : IFileService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FileService> _logger;

    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private static readonly string[] AllowedVideoExtensions = { ".mp4", ".webm" };
    private const long MaxVideoSizeBytes = 100 * 1024 * 1024; // 15 MB
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

    public async Task<string> SaveVideoAsync(IFormFile file, string folder)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("Video file is empty.");

        if (file.Length > MaxVideoSizeBytes)
            throw new InvalidOperationException("Video is too large. Max 100 MB allowed.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedVideoExtensions.Contains(ext))
            throw new InvalidOperationException(
                $"Invalid video type. Allowed: {string.Join(", ", AllowedVideoExtensions)}");

        var now = DateTime.UtcNow;
        var relativeFolder = Path.Combine("uploads", folder, "videos", now.ToString("yyyy"), now.ToString("MM"));
        var physicalFolder = Path.Combine(_env.WebRootPath, relativeFolder);
        Directory.CreateDirectory(physicalFolder);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var physicalPath = Path.Combine(physicalFolder, fileName);

        await using (var stream = new FileStream(physicalPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var urlPath = "/" + relativeFolder.Replace("\\", "/") + "/" + fileName;
        _logger.LogInformation("Saved uploaded video to {Path}", urlPath);
        return urlPath;
    }

}