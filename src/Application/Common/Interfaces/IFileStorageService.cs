using Microsoft.AspNetCore.Http;

namespace AskAMuslimBackend.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadFileAsync(IFormFile file, string containerName, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(string fileUrl, string containerName, CancellationToken cancellationToken = default);
    Task<Stream> DownloadFileAsync(string fileUrl, string containerName, CancellationToken cancellationToken = default);
}
