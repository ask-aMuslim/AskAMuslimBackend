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
        Guid? studentId = null;
        if (Guid.TryParse(_currentUser.Id, out var currentUserId))
        {
            studentId = await _context.Students
                .Where(s => s.UserId == currentUserId)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        LessonBriefDto? lesson;
        if (studentId.HasValue)
        {
            var sid = studentId.Value;
            lesson = await _context.Lessons
                .Where(l => l.Id == request.Id)
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
                    IsComplete = _context.StudentProgresses.Any(sp => sp.StudentId == sid && sp.LessonId == s.Id && sp.IsCompleted),
                    Language = s.Language
                })
                .FirstOrDefaultAsync(cancellationToken);
        }
        else
        {
            lesson = await _context.Lessons
                .Where(l => l.Id == request.Id)
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
                    IsComplete = false,
                    Language = s.Language
                })
                .FirstOrDefaultAsync(cancellationToken);
        }
        
        return Result<LessonBriefDto?>.Success(lesson);
    }
}
