using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace AskAMuslimBackend.Application.Media.Commands.UploadAttachment;

public record UploadAttachmentCommand : IRequest<Result<string>>
{
    public IFormFile File { get; init; } = null!;
    public string ContainerName { get; init; } = "attachments";
}

public class UploadAttachmentCommandHandler : IRequestHandler<UploadAttachmentCommand, Result<string>>
{
    private readonly IFileStorageService _fileStorageService;

    public UploadAttachmentCommandHandler(IFileStorageService fileStorageService)
    {
        _fileStorageService = fileStorageService;
    }

    public async Task<Result<string>> Handle(UploadAttachmentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var url = await _fileStorageService.UploadFileAsync(
                request.File,
                request.ContainerName,
                cancellationToken);

            return Result<string>.Success(url);
        }
        catch (ArgumentException ex)
        {
            return Result<string>.Failure(new[] { ex.Message });
        }
        catch (Exception ex)
        {
            return Result<string>.Failure(new[] { $"Failed to upload attachment: {ex.Message}" });
        }
    }
}