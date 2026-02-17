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

public class UploadAttachmentCommandValidator : AbstractValidator<UploadAttachmentCommand>
{
    // Image extensions
    private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp", ".svg" };
    private static readonly string[] ImageContentTypes = { "image/jpeg", "image/png", "image/webp", "image/gif", "image/bmp", "image/svg+xml" };
    
    // Video extensions
    private static readonly string[] VideoExtensions = { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".mkv", ".webm", ".m4v", ".mpeg", ".mpg" };
    private static readonly string[] VideoContentTypes = { "video/mp4", "video/x-msvideo", "video/quicktime", "video/x-ms-wmv", "video/x-flv", "video/x-matroska", "video/webm", "video/mpeg" };
    
    // Audio extensions
    private static readonly string[] AudioExtensions = { ".mp3", ".wav", ".aac", ".ogg", ".flac", ".m4a", ".wma", ".aiff" };
    private static readonly string[] AudioContentTypes = { "audio/mpeg", "audio/wav", "audio/aac", "audio/ogg", "audio/flac", "audio/x-m4a", "audio/x-ms-wma", "audio/aiff" };
    
    // Document extensions
    private static readonly string[] DocumentExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".rtf", ".odt", ".ods", ".odp" };
    private static readonly string[] DocumentContentTypes = { 
        "application/pdf", 
        "application/msword", 
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "text/plain",
        "application/rtf",
        "application/vnd.oasis.opendocument.text",
        "application/vnd.oasis.opendocument.spreadsheet",
        "application/vnd.oasis.opendocument.presentation"
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

    public UploadAttachmentCommandValidator()
    {
        RuleFor(x => x.File)
            .NotNull().WithMessage("File is required.")
            .Must(HaveValidExtension).WithMessage($"File must have a valid extension. Allowed: images ({string.Join(", ", ImageExtensions)}), videos ({string.Join(", ", VideoExtensions)}), audio ({string.Join(", ", AudioExtensions)}), documents ({string.Join(", ", DocumentExtensions)})")
            .Must(HaveValidContentType).WithMessage("File must have a valid content type.")
            .Must(BeWithinSizeLimit).WithMessage(GetSizeLimitMessage);

        RuleFor(x => x.ContainerName)
            .NotEmpty().WithMessage("Container name is required.");
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

    private string GetSizeLimitMessage(UploadAttachmentCommand command)
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
}