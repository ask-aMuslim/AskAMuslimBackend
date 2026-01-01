using AskAMuslimBackend.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace AskAMuslimBackend.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly string _uploadPath;

    public FileStorageService(IConfiguration configuration)
    {
        _uploadPath = configuration["FileStorage:UploadPath"] ?? "/var/opt/Uploads";
        
        // Ensure the directory exists
        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
        }
    }

    public async Task<string> UploadFileAsync(IFormFile file, string containerName, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is empty or null", nameof(file));
        }

        // Create container directory if it doesn't exist
        var containerPath = Path.Combine(_uploadPath, containerName);
        if (!Directory.Exists(containerPath))
        {
            Directory.CreateDirectory(containerPath);
        }

        // Generate unique filename
        var fileExtension = Path.GetExtension(file.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(containerPath, uniqueFileName);

        // Save file to disk
        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream, cancellationToken);

        // Return relative path from upload directory
        return $"/uploads/{containerName}/{uniqueFileName}";
    }

    public Task<bool> DeleteFileAsync(string fileUrl, string containerName, CancellationToken cancellationToken = default)
    {
        try
        {
            var fileName = Path.GetFileName(fileUrl);
            var filePath = Path.Combine(_uploadPath, containerName, fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<Stream> DownloadFileAsync(string fileUrl, string containerName, CancellationToken cancellationToken = default)
    {
        var fileName = Path.GetFileName(fileUrl);
        var filePath = Path.Combine(_uploadPath, containerName, fileName);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found", filePath);
        }

        var stream = File.OpenRead(filePath);
        return Task.FromResult<Stream>(stream);
    }
}
