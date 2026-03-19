using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Courses.Queries.GetCourseById;

public record GetCourseByIdQuery(Guid Id) : IRequest<Result<CourseBriefDto?>>;

public class GetCourseByIdQueryHandler : IRequestHandler<GetCourseByIdQuery, Result<CourseBriefDto?>>
{
    private readonly IApplicationDbContext _context;

    public GetCourseByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<CourseBriefDto?>> Handle(GetCourseByIdQuery request, CancellationToken cancellationToken)
    {
        var course = await _context.Courses
            .Include(c => c.CourseTags)
                .ThenInclude(ct => ct.Tag)
            .Where(c => c.Id == request.Id)
            .Select(s => new CourseBriefDto {
            Id = s.Id,
            Title = s.Title,
            Description = s.Description,
            ThumbnailUrl = s.ThumbnailUrl,
            IsPublished = s.IsPublished,
            Rating = s.Rating,
            Order = s.Order,
            Language = s.Language,
            Tags = s.CourseTags.Select(ct => new AskAMuslimBackend.Application.Tags.Queries.TagBriefDto { Id = ct.Tag!.Id, Title = ct.Tag.Title, Description = ct.Tag.Description, TotalCourses = ct.Tag.CourseTags.Count, TotalQAs = ct.Tag.QATags.Count }).ToList()
    })
            .FirstOrDefaultAsync(cancellationToken);
            
        return Result<CourseBriefDto?>.Success(course);
    }
}
