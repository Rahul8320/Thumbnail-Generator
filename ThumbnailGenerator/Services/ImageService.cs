using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ThumbnailGenerator.Services;

public sealed class ImageService
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif"];
    private static readonly string[] AllowedMimeTypes = ["image/jpeg", "image/png", "image/gif"];

    public static readonly int[] ThumbnailWidths = [32,64,128,256,512,1024];

    public bool IsValidImage(IFormFile file)
    {
        if (file.Length == 0)
        {
            return false;
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return AllowedExtensions.Contains(extension) && AllowedMimeTypes.Contains(file.ContentType);
    }

    public async Task<string> SaveOriginalImageAsync(IFormFile file, string folderPath, string fileName)
    {
        var originalFilePath = Path.Combine(folderPath, fileName);
        Directory.CreateDirectory(folderPath);

        using var stream = new FileStream(originalFilePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return originalFilePath;
    }

    public async Task<IEnumerable<string>> GenerateThumbnailsAsync(
        string originalFilePath, 
        string folderPath, 
        string fileNameWithoutExtension, 
        int[]? widths = null)
    {
        var thumbnailPaths = new List<string>();
        var extension = Path.GetExtension(originalFilePath);
        widths ??= ThumbnailWidths;

        using var image = await Image.LoadAsync(originalFilePath);

        foreach (var width in widths)
        {
            var thumbnailFileName = $"{fileNameWithoutExtension}_w{width}{extension}";
            var thumbnailPath = Path.Combine(folderPath, thumbnailFileName);

            var resizedImage = image.Clone(x => x.Resize(width, 0)); // 0 height maintains aspect ratio
            await resizedImage.SaveAsync(thumbnailPath);

            thumbnailPaths.Add(thumbnailFileName);
        }

        return thumbnailPaths;
    }

    public IReadOnlyList<string> GetMatchingFiles(string folderPath, string id,  int? width = null)
    {
        // Build file name pattern
        string filePattern = width.HasValue
            ? $"{id}_w{width.Value}.*"  // Thumbnail file pattern
            : $"{id}.*";               // Original file pattern

        // Search for the file in supported formats
        var matchingFiles = Directory.GetFiles(folderPath, filePattern)
                                     .Where(file => AllowedExtensions.Contains(Path.GetExtension(file).ToLower()))
                                     .ToList();

      return matchingFiles.AsReadOnly();
    }
}
