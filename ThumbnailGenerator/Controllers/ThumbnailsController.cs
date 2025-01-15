using Microsoft.AspNetCore.Mvc;
using ThumbnailGenerator.Services;

namespace ThumbnailGenerator.Controllers;

[Route("api/[controller]")]
[ApiController]
public sealed class ThumbnailsController(
    ImageService imageService, 
    LinkGenerator linkGenerator) : ControllerBase
{
    private readonly string _uploadDirectory = "uploads";

    [HttpPost]
    public async Task<IActionResult> UploadImage(IFormFile? file)
    {
        if (file is null)
        {
            return BadRequest("No file uploaded!");
        }
        
        if (imageService.IsValidImage(file) == false)
        {
            return BadRequest("Invalid image file. Only JPG, PNG, and GIF formats are supported!");
        }

        var folderName = Guid.NewGuid().ToString();
        var folderPath = Path.Combine(_uploadDirectory, "images", folderName);
        var fileName = $"{folderName}{Path.GetExtension(file.FileName)}";

        var originalFilePath = await imageService.SaveOriginalImageAsync(file, folderPath, fileName);
        await imageService.GenerateThumbnailsAsync(originalFilePath, folderPath, folderName);

        var thumbnailLinks = ImageService.ThumbnailWidths.ToDictionary(
            width => $"w{width}",
            width => GetFullyQualifiedUrl(nameof(GetImage), new { id = folderName, width}));

        thumbnailLinks.Add("original", GetFullyQualifiedUrl(nameof(GetImage), new {id = folderName }));

        return Ok(new { id = folderName, links =  thumbnailLinks });
    }

    [HttpGet("{id}")]
    public IActionResult GetImage(string id,[FromQuery] int? width = null)
    {
        try
        {
            var folderPath = Path.Combine(_uploadDirectory, "images", id);

            if (Directory.Exists(folderPath) == false)
            {
                return NotFound();
            }

            var matchingFiles = imageService.GetMatchingFiles(folderPath, id, width);

            if (matchingFiles.Count == 0)
            {
                return NotFound(new { message = "Image not found" });
            }

            var filePath = matchingFiles[0];
            var mimeType = GetMimeType(filePath);

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, mimeType);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred", details = ex.Message });
        }
    }

    private string GetFullyQualifiedUrl(string actionName, object values)
    {
        return linkGenerator.GetUriByAction(
            HttpContext,
            action: actionName,
            controller: "Thumbnails",
            values) ?? throw new InvalidOperationException("Failed to generate URL!");
    }
    private static string GetMimeType(string filePath)
    {
        // Determine MIME type based on file extension
        var extension = Path.GetExtension(filePath).ToLower();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };
    }
}
