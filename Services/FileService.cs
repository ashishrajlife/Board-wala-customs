using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace ValousWorld.Web.Services;

public class FileService : IFileService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FileService> _logger;

    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private static readonly string[] AllowedVideoExtensions = { ".mp4", ".webm" };
    private const long MaxVideoSizeBytes = 100 * 1024 * 1024; // 100 MB
    private const long MaxFileSizeBytes = 100 * 1024 * 1024; // 100 MB

    public FileService(IWebHostEnvironment env, ILogger<FileService> logger)
    {
        _env = env;
        _logger = logger;
    }

    // ============================================================
    // IMAGES — Auto-convert to WebP & Resize for faster loading
    // ============================================================
        public async Task<string> SaveImageAsync(IFormFile file, string folder)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidOperationException("Invalid image type.");

        var now = DateTime.UtcNow;
        var relativeFolder = Path.Combine("uploads", folder, now.ToString("yyyy"), now.ToString("MM"));
        var physicalFolder = Path.Combine(_env.WebRootPath, relativeFolder);
        Directory.CreateDirectory(physicalFolder);

        var baseName = Guid.NewGuid().ToString("N");
        var fileName = $"{baseName}.webp";
        var physicalPath = Path.Combine(physicalFolder, fileName);

        try
        {
            // Reading file to MemoryStream makes it faster to process
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var image = await Image.LoadAsync(stream);
            
            image.Mutate(x => x.AutoOrient().Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(1200, 1200)
            }));

            await image.SaveAsWebpAsync(physicalPath, new WebpEncoder
            {
                Quality = 80,
                FileFormat = WebpFileFormatType.Lossy
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Image processing failed");
            throw new InvalidOperationException("Image could not be processed.");
        }

        return "/" + relativeFolder.Replace("\\", "/") + "/" + fileName;
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

    // ============================================================
    // VIDEOS — Save as is (No conversion)
    // ============================================================
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