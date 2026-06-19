using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Lessons.Queries.GetLessonById;

public record GetLessonByIdQuery(Guid Id) : IRequest<Result<LessonBriefDto?>>;

public class GetLessonByIdQueryHandler : IRequestHandler<GetLessonByIdQuery, Result<LessonBriefDto?>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public GetLessonByIdQueryHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<LessonBriefDto?>> Handle(GetLessonByIdQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var currentUserId))
            return Result<LessonBriefDto?>.Failure(["Unauthorized"]);

        var studentId = await _context.Students
            .AsNoTracking()
            .Where(s => s.UserId == currentUserId)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (!studentId.HasValue)
            return Result<LessonBriefDto?>.Failure(["Student profile not found."]);

        var sid = studentId.Value;

        var lesson = await _context.Lessons
            .AsNoTracking()
            .Where(l => l.Id == request.Id && l.IsPublished)
            .Select(s => new LessonBriefDto
            {
                Id = s.Id,
                CourseId = s.CourseId,
                Title = s.Title,
                Description = s.Description,
                ContentJson = s.ContentJson,
                Type = s.Type,
                Order = s.Order,
                ThumbnailUrl = s.ThumbnailUrl,
                ContentUrl = s.ContentUrl,
                Transcript = s.Transcript,
                IsPublished = s.IsPublished,
                HasQuiz = s.HasQuiz,
                Language = s.Language
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (lesson is null)
            return Result<LessonBriefDto?>.Failure(["Lesson not found or not published."]);

        var progress = await _context.StudentProgresses
            .AsNoTracking()
            .Where(sp => sp.StudentId == sid && sp.LessonId == lesson.Id)
            .Select(sp => new { sp.IsCompleted, sp.ProgressPercentage })
            .FirstOrDefaultAsync(cancellationToken);

        lesson.IsComplete = progress?.IsCompleted ?? false;

        return Result<LessonBriefDto?>.Success(lesson);
    }
}
