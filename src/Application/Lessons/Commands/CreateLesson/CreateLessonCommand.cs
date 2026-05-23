using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Validators;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Lessons.Commands.CreateLesson;

public record CreateLessonCommand : IRequest<Result<Guid>>
{
    public Guid CourseId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ContentJson { get; init; }
    public LessonType Type { get; init; } = LessonType.Video;
    public string? ContentUrl { get; init; }
    public string? Transcript { get; init; }
    public string? ThumbnailUrl { get; init; }
    public int Order { get; init; }
    public TimeSpan? Duration { get; init; }
    public bool IsPublished { get; init; } = false;
    public Language Language { get; init; } = Language.English;
}

public class CreateLessonCommandHandler : IRequestHandler<CreateLessonCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILocalizationService _localizer;
    private readonly IYouTubeService _youTubeService;

    public CreateLessonCommandHandler(IApplicationDbContext context, ILocalizationService localizer, IYouTubeService youTubeService)
    {
        _context = context;
        _localizer = localizer;
        _youTubeService = youTubeService;
    }

    public async Task<Result<Guid>> Handle(CreateLessonCommand request, CancellationToken cancellationToken)
    {
        // Defensive existence checks (in addition to FluentValidation) to avoid FK violation surfacing as DbUpdateException.
        var courseExists = await _context.Courses.AsNoTracking().AnyAsync(c => c.Id == request.CourseId, cancellationToken);
        if (!courseExists)
        {
            return Result<Guid>.Failure(new[] { _localizer["Courses.DoesNotExist"] });
        }

        var duration = request.Duration;
        if (!duration.HasValue && !string.IsNullOrWhiteSpace(request.ContentUrl))
        {
            if (request.Type == LessonType.Video)
            {
                var videoId = ExtractYouTubeVideoId(request.ContentUrl);
                if (!string.IsNullOrEmpty(videoId))
                {
                    duration = await _youTubeService.FetchVideoDurationAsync(videoId, cancellationToken);
                }
            }
            else if (request.Type == LessonType.Audio)
            {
                duration = await _youTubeService.FetchAudioDurationAsync(request.ContentUrl, cancellationToken);
            }
        }

        var lesson = new Lesson
        {
            CourseId = request.CourseId,
            Title = request.Title,
            Description = request.Description,
            ContentJson = request.ContentJson,
            Type = request.Type,
            ContentUrl = request.ContentUrl,
            Transcript = request.Transcript,
            ThumbnailUrl = request.ThumbnailUrl,
            Order = request.Order,
            Duration = duration,
            IsPublished = request.IsPublished,
            Language = request.Language
        };

        _context.Lessons.Add(lesson);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(lesson.Id);
    }

    private static string? ExtractYouTubeVideoId(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        // youtu.be/{id}
        var shortMatch = System.Text.RegularExpressions.Regex.Match(url, @"youtu\.be/([a-zA-Z0-9_-]{11})");
        if (shortMatch.Success) return shortMatch.Groups[1].Value;
        // v={id}
        var normalMatch = System.Text.RegularExpressions.Regex.Match(url, @"[?&]v=([a-zA-Z0-9_-]{11})");
        if (normalMatch.Success) return normalMatch.Groups[1].Value;
        // /embed/{id}
        var embedMatch = System.Text.RegularExpressions.Regex.Match(url, @"youtube\.com/embed/([a-zA-Z0-9_-]{11})");
        if (embedMatch.Success) return embedMatch.Groups[1].Value;
        return null;
    }
}

public class CreateLessonCommandValidator : AbstractValidator<CreateLessonCommand>
{
    private readonly IApplicationDbContext _context;

    public CreateLessonCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.CourseId)
            .NotEmpty().WithMessage("Course is required.")
            .MustAsync(CourseExists).WithMessage("Course does not exist.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.ContentUrl)
            .MaximumLength(500).WithMessage("Content URL must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.ContentUrl));

        RuleFor(x => x.ThumbnailUrl)
            .MaximumLength(500).WithMessage("Thumbnail URL must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.ThumbnailUrl));

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid lesson type.");

        RuleFor(x => x.Language)
            .IsInEnum().WithMessage("Invalid language value.");

        RuleFor(x => x.ContentJson)
            .Must(BeValidTiptapJson).WithMessage(x => GetTiptapJsonError(x.ContentJson))
            .When(x => !string.IsNullOrEmpty(x.ContentJson));

        RuleFor(x => x.Order)
            .GreaterThanOrEqualTo(0).WithMessage("Order must be 0 or greater.");
    }

    private async Task<bool> CourseExists(Guid courseId, CancellationToken ct)
        => await _context.Courses.AnyAsync(c => c.Id == courseId, ct);

    private bool BeValidTiptapJson(string? json)
        => TiptapJsonValidator.IsValidTiptapJson(json, out _);

    private string GetTiptapJsonError(string? json)
    {
        TiptapJsonValidator.IsValidTiptapJson(json, out var error);
        return error ?? "Invalid Tiptap JSON.";
    }
}
