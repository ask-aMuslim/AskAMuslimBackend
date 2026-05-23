using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Validators;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;
using System.Text.Json.Serialization;

namespace AskAMuslimBackend.Application.Lessons.Commands.UpdateLesson;

public record UpdateLessonCommand : IRequest<Result>
{
    [JsonIgnore] // Set from route parameter, not request body
    public Guid Id { get; init; }
    public Guid CourseId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ContentJson { get; init; }
    public LessonType Type { get; init; } = LessonType.Video;
    public string? ContentUrl { get; init; }
    public string? Transcript { get; init; }
    public string? ThumbnailUrl { get; init; }
    public TimeSpan? Duration { get; init; }
    public int Order { get; init; }
    public bool IsPublished { get; init; }
    public Language Language { get; init; } = Language.English;
}

public class UpdateLessonCommandHandler : IRequestHandler<UpdateLessonCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ILocalizationService _localizer;
    private readonly IYouTubeService _youTubeService;

    public UpdateLessonCommandHandler(IApplicationDbContext context, ILocalizationService localizer, IYouTubeService youTubeService)
    {
        _context = context;
        _localizer = localizer;
        _youTubeService = youTubeService;
    }

    public async Task<Result> Handle(UpdateLessonCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Lessons.FindAsync(new object[] { request.Id }, cancellationToken);
        
        if (entity == null)
            return Result.Failure(new[] { _localizer["Lessons.NotFound"] });

        // Optional: ensure course relation not broken
        if (entity.CourseId != request.CourseId)
        {
            // Ensure target course exists
            var course = await _context.Courses.FindAsync(new object[] { request.CourseId }, cancellationToken);
            if (course == null)
                return Result.Failure(new[] { _localizer["Courses.NotFound"] });
            entity.CourseId = request.CourseId;
        }

        var duration = request.Duration;
        if (request.Type == LessonType.Video && !string.IsNullOrWhiteSpace(request.ContentUrl))
        {
            var videoId = ExtractYouTubeVideoId(request.ContentUrl);
            if (!string.IsNullOrEmpty(videoId))
            {
                duration = await _youTubeService.FetchVideoDurationAsync(videoId, cancellationToken);
            }
        }

        entity.Title = request.Title;
        entity.Description = request.Description;
        entity.ContentJson = request.ContentJson;
        entity.Type = request.Type;
        entity.ContentUrl = request.ContentUrl;
        entity.Transcript = request.Transcript;
        entity.ThumbnailUrl = request.ThumbnailUrl;
        entity.Duration = duration;
        entity.Order = request.Order;
        entity.IsPublished = request.IsPublished;
        entity.Language = request.Language;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static string? ExtractYouTubeVideoId(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var shortMatch = System.Text.RegularExpressions.Regex.Match(url, @"youtu\.be/([a-zA-Z0-9_-]{11})");
        if (shortMatch.Success) return shortMatch.Groups[1].Value;
        var normalMatch = System.Text.RegularExpressions.Regex.Match(url, @"[?&]v=([a-zA-Z0-9_-]{11})");
        if (normalMatch.Success) return normalMatch.Groups[1].Value;
        var embedMatch = System.Text.RegularExpressions.Regex.Match(url, @"youtube\.com/embed/([a-zA-Z0-9_-]{11})");
        if (embedMatch.Success) return embedMatch.Groups[1].Value;
        return null;
    }
}

public class UpdateLessonCommandValidator : AbstractValidator<UpdateLessonCommand>
{
    public UpdateLessonCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => !string.IsNullOrEmpty(x.Description));
        RuleFor(x => x.ContentUrl).MaximumLength(500).When(x => !string.IsNullOrEmpty(x.ContentUrl));
        RuleFor(x => x.ThumbnailUrl).MaximumLength(500).When(x => !string.IsNullOrEmpty(x.ThumbnailUrl));
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Language).IsInEnum().WithMessage("Invalid language value.");
        RuleFor(x => x.Order).GreaterThanOrEqualTo(0);

        RuleFor(x => x.ContentJson)
            .Must(BeValidTiptapJson).WithMessage(x => GetTiptapJsonError(x.ContentJson))
            .When(x => !string.IsNullOrEmpty(x.ContentJson));
    }

    private bool BeValidTiptapJson(string? json)
        => TiptapJsonValidator.IsValidTiptapJson(json, out _);

    private string GetTiptapJsonError(string? json)
    {
        TiptapJsonValidator.IsValidTiptapJson(json, out var error);
        return error ?? "Invalid Tiptap JSON.";
    }
}
