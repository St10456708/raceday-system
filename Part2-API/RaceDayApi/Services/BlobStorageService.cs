using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDayApi.Services;

public interface IBlobStorageService
{
    Task<string> UploadImageAsync(IFormFile file, string folder);
}

public class BlobStorageService : IBlobStorageService
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private const long MaxBytes = 5 * 1024 * 1024;
    private readonly IConfiguration _config;

    public BlobStorageService(IConfiguration config)
    {
        _config = config;
    }

    public async Task<string> UploadImageAsync(IFormFile file, string folder)
    {
        var connectionString = _config["AzureBlob:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Azure Blob Storage is not configured.");
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (file.Length == 0 || file.Length > MaxBytes || !AllowedExtensions.Contains(ext))
        {
            throw new ArgumentException("Please upload a .jpg, .png, .webp or .gif image smaller than 5 MB.");
        }

        var container = new BlobContainerClient(connectionString, _config["AzureBlob:ContainerName"] ?? "raceday-images");
        await container.CreateIfNotExistsAsync();

        var blob = container.GetBlobClient($"{folder}/{Guid.NewGuid():N}{ext}");
        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType }
        });

        return blob.Uri.ToString();
    }
}
