using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace AskAMuslimBackend.Application.Media.Commands.UploadImage;

public record UploadImageCommand : IRequest<Result<string>>
{
    public IFormFile File { get; init; } = null!;
    public string ContainerName { get; init; } = "lesson-images";
}

public class UploadImageCommandHandler : IRequestHandler<UploadImageCommand, Result<string>>
{
    private readonly IFileStorageService _fileStorageService;

    public UploadImageCommandHandler(IFileStorageService fileStorageService)
    {
        _fileStorageService = fileStorageService;
    }

    public async Task<Result<string>> Handle(UploadImageCommand request, CancellationToken cancellationToken)
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
            return Result<string>.Failure(new[] { $"Failed to upload image: {ex.Message}" });
        }
    }
}

public class UploadImageCommandValidator : AbstractValidator<UploadImageCommand>
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/webp", "image/gif" };
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

    public UploadImageCommandValidator()
    {
        RuleFor(x => x.File)
            .NotNull().WithMessage("File is required.")
            .Must(HaveValidExtension).WithMessage($"File must have one of the following extensions: {string.Join(", ", AllowedExtensions)}")
            .Must(HaveValidContentType).WithMessage($"File must be one of the following types: {string.Join(", ", AllowedContentTypes)}")
            .Must(BeWithinSizeLimit).WithMessage($"File size must not exceed {MaxFileSize / 1024 / 1024}MB.");

        RuleFor(x => x.ContainerName)
            .NotEmpty().WithMessage("Container name is required.")
            .Must(BeValidContainerName).WithMessage("Invalid container name. Allowed: lesson-images, qa-images");
    }

    private bool HaveValidExtension(IFormFile? file)
    {
        if (file == null) return false;
        var extension = Path.GetExtension(file.FileName).ToLower();
        return AllowedExtensions.Contains(extension);
    }

    private bool HaveValidContentType(IFormFile? file)
    {
        if (file == null) return false;
        return AllowedContentTypes.Contains(file.ContentType.ToLower());
    }

    private bool BeWithinSizeLimit(IFormFile? file)
    {
        if (file == null) return false;
        return file.Length <= MaxFileSize;
    }

    private bool BeValidContainerName(string containerName)
    {
        var allowedContainers = new[] { "lesson-images", "qa-images" };
        return allowedContainers.Contains(containerName.ToLower());
    }
}
