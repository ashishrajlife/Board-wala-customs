namespace ValousWorld.Web.Services;

public interface IFileService
{
    /// <summary>
    /// Saves an uploaded image under wwwroot/uploads/{folder}/{yyyy}/{MM}/.
    /// Returns the relative URL like "/uploads/products/2026/09/abc.jpg".
    /// </summary>
    Task<string> SaveImageAsync(IFormFile file, string folder);

    /// <summary>
    /// Deletes a previously saved image given its relative URL.
    /// Silently does nothing if the file doesn't exist or URL is external.
    /// </summary>
    void DeleteImage(string? relativeUrl);
}