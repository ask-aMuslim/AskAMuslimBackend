using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace AskAMuslimBackend.Application.Lessons.Commands.SetLessonDuration;

/// <summary>
/// Command to resolve or manually set a lesson's Duration.
/// - If ManualDuration is provided, it is applied directly (admin override).
/// - If ManualDuration is null, auto-resolve based on LessonType:
///   - Video: resolves from YouTube API via ContentUrl.
///   - Audio: resolves from YouTube API if the URL is YouTube; for direct audio files returns null.
///   - Article / Document: falls back to default 5 minutes.
/// - If auto-resolution returns null for Video/Audio and no manual duration was given, the endpoint returns a warning.
/// </summary>
public record SetLessonDurationCommand : IRequest<Result>
{
    public Guid LessonId { get; init; }
    /// <summary>
    /// Optional manual duration override. When provided, auto-resolution is skipped.
    /// </summary>
    public TimeSpan? ManualDuration { get; init; }
}

public class SetLessonDurationCommandHandler : IRequestHandler<SetLessonDurationCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IYouTubeService _youTubeService;

    private static readonly TimeSpan DefaultContentDuration = new TimeSpan(0, 5, 0); // 5 minutes

    public SetLessonDurationCommandHandler(IApplicationDbContext context, IYouTubeService youTubeService)
    {
        _context = context;
        _youTubeService = youTubeService;
    }

    public async Task<Result> Handle(SetLessonDurationCommand request, CancellationToken cancellationToken)
    {
        var lesson = await _context.Lessons
            .FirstOrDefaultAsync(l => l.Id == request.LessonId, cancellationToken);

        if (lesson == null)
            return Result.Failure(new[] { "Lesson not found." });

        TimeSpan? resolvedDuration;

        if (request.ManualDuration.HasValue)
        {
            resolvedDuration = request.ManualDuration.Value;
        }
        else
        {
            resolvedDuration = await AutoResolveDurationAsync(lesson, cancellationToken);

            if (!resolvedDuration.HasValue)
            {
                return Result.Failure(new[]
                {
                    $"Could not auto-resolve duration for lesson '{lesson.Title}' (Type: {lesson.Type}). " +
                    "Please provide a manual duration."
                });
            }
        }

        lesson.Duration = resolvedDuration;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private async Task<TimeSpan?> AutoResolveDurationAsync(Domain.Entities.Lesson lesson, CancellationToken cancellationToken)
    {
        switch (lesson.Type)
        {
            case LessonType.Video:
                if (!string.IsNullOrWhiteSpace(lesson.ContentUrl))
                {
                    var videoId = ExtractYouTubeVideoId(lesson.ContentUrl);
                    if (!string.IsNullOrEmpty(videoId))
                    {
                        return await _youTubeService.FetchVideoDurationAsync(videoId, cancellationToken);
                    }
                }
                return null;

            case LessonType.Audio:
                if (!string.IsNullOrWhiteSpace(lesson.ContentUrl))
                {
                    return await _youTubeService.FetchAudioDurationAsync(lesson.ContentUrl, cancellationToken);
                }
                return null;

            case LessonType.Article:
            case LessonType.Document:
                return DefaultContentDuration;

            default:
                return null;
        }
    }

    private static string? ExtractYouTubeVideoId(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var shortMatch = Regex.Match(url, @"youtu\.be/([a-zA-Z0-9_-]{11})");
        if (shortMatch.Success) return shortMatch.Groups[1].Value;
        var normalMatch = Regex.Match(url, @"[?&]v=([a-zA-Z0-9_-]{11})");
        if (normalMatch.Success) return normalMatch.Groups[1].Value;
        var embedMatch = Regex.Match(url, @"youtube\.com/embed/([a-zA-Z0-9_-]{11})");
        if (embedMatch.Success) return embedMatch.Groups[1].Value;
        return null;
    }
}

public class SetLessonDurationCommandValidator : AbstractValidator<SetLessonDurationCommand>
{
    public SetLessonDurationCommandValidator()
    {
        RuleFor(x => x.LessonId).NotEmpty();
        RuleFor(x => x.ManualDuration)
            .Must(d => d == null || d.Value > TimeSpan.Zero)
            .WithMessage("Manual duration must be greater than zero.")
            .When(x => x.ManualDuration.HasValue);
    }
}
