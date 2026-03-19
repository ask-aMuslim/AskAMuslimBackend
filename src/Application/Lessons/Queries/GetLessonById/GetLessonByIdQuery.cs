using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Lessons.Queries.GetLessonById;

public record GetLessonByIdQuery(Guid Id) : IRequest<Result<LessonBriefDto?>>;

public class GetLessonByIdQueryHandler : IRequestHandler<GetLessonByIdQuery, Result<LessonBriefDto?>>
{
    private readonly IApplicationDbContext _context;

    public GetLessonByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<LessonBriefDto?>> Handle(GetLessonByIdQuery request, CancellationToken cancellationToken)
    {
        var lesson = await _context.Lessons
            .Where(l => l.Id == request.Id)
            .Select(s => new LessonBriefDto {
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
            Language = s.Language
    })
            .FirstOrDefaultAsync(cancellationToken);
        
        return Result<LessonBriefDto?>.Success(lesson);
    }
}
