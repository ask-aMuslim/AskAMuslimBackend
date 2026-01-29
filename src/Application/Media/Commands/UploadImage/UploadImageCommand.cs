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
    // Image extensions
    private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp", ".svg" };
    private static readonly string[] ImageContentTypes = { "image/jpeg", "image/png", "image/webp", "image/gif", "image/bmp", "image/svg+xml" };
    
    // Video extensions
    private static readonly string[] VideoExtensions = { ".mp4", ".webm", ".mov", ".mkv" };
    private static readonly string[] VideoContentTypes = { "video/mp4", "video/webm", "video/quicktime", "video/x-matroska" };
    
    // Audio extensions
    private static readonly string[] AudioExtensions = { ".mp3", ".wav", ".m4a", ".aac", ".ogg" };
    private static readonly string[] AudioContentTypes = { "audio/mpeg", "audio/wav", "audio/x-m4a", "audio/aac", "audio/ogg" };
    
    // Document extensions
    private static readonly string[] DocumentExtensions = { ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx" };
    private static readonly string[] DocumentContentTypes = { 
        "application/pdf", 
        "application/msword", 
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
    };

    // Combined allowed extensions and content types
    private static readonly string[] AllowedExtensions = ImageExtensions
        .Concat(VideoExtensions)
        .Concat(AudioExtensions)
        .Concat(DocumentExtensions)
        .ToArray();

    private static readonly string[] AllowedContentTypes = ImageContentTypes
        .Concat(VideoContentTypes)
        .Concat(AudioContentTypes)
        .Concat(DocumentContentTypes)
        .ToArray();

    // File size limits
    private const long MaxImageSize = 10 * 1024 * 1024; // 10MB for images
    private const long MaxVideoSize = 500 * 1024 * 1024; // 500MB for videos
    private const long MaxAudioSize = 50 * 1024 * 1024; // 50MB for audio
    private const long MaxDocumentSize = 50 * 1024 * 1024; // 50MB for documents

    public UploadImageCommandValidator()
    {
        RuleFor(x => x.File)
            .NotNull().WithMessage("File is required.")
            .Must(HaveValidExtension).WithMessage($"File must have a valid extension. Allowed: images ({string.Join(", ", ImageExtensions)}), videos ({string.Join(", ", VideoExtensions)}), audio ({string.Join(", ", AudioExtensions)}), documents ({string.Join(", ", DocumentExtensions)})")
            .Must(HaveValidContentType).WithMessage("File must have a valid content type.")
            .Must(BeWithinSizeLimit).WithMessage(GetSizeLimitMessage);

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
        
        var extension = Path.GetExtension(file.FileName).ToLower();
        
        if (ImageExtensions.Contains(extension))
            return file.Length <= MaxImageSize;
        
        if (VideoExtensions.Contains(extension))
            return file.Length <= MaxVideoSize;
        
        if (AudioExtensions.Contains(extension))
            return file.Length <= MaxAudioSize;
        
        if (DocumentExtensions.Contains(extension))
            return file.Length <= MaxDocumentSize;
        
        return false;
    }

    private string GetSizeLimitMessage(UploadImageCommand command)
    {
        if (command.File == null) return "File size validation failed.";
        
        var extension = Path.GetExtension(command.File.FileName).ToLower();
        
        if (ImageExtensions.Contains(extension))
            return $"Image file size must not exceed {MaxImageSize / 1024 / 1024}MB.";
        
        if (VideoExtensions.Contains(extension))
            return $"Video file size must not exceed {MaxVideoSize / 1024 / 1024}MB.";
        
        if (AudioExtensions.Contains(extension))
            return $"Audio file size must not exceed {MaxAudioSize / 1024 / 1024}MB.";
        
        if (DocumentExtensions.Contains(extension))
            return $"Document file size must not exceed {MaxDocumentSize / 1024 / 1024}MB.";
        
        return "File size validation failed.";
    }

    private bool BeValidContainerName(string containerName)
    {
        var allowedContainers = new[] { "lesson-images", "qa-images" };
        return allowedContainers.Contains(containerName.ToLower());
    }
}
