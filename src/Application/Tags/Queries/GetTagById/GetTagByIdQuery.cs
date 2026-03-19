using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Tags.Queries.GetTagById;

public record GetTagByIdQuery(Guid Id) : IRequest<Result<TagBriefDto?>>;

public class GetTagByIdQueryHandler : IRequestHandler<GetTagByIdQuery, Result<TagBriefDto?>>
{
    private readonly IApplicationDbContext _context;

    public GetTagByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<TagBriefDto?>> Handle(GetTagByIdQuery request, CancellationToken cancellationToken)
    {
        var tag = await _context.Tags
            .Where(c => c.Id == request.Id)
            .Select(s => new TagBriefDto {
            Id = s.Id,
            Title = s.Title,
            Description = s.Description,
            TotalCourses = s.CourseTags.Count,
            TotalQAs = s.QATags.Count
    })
            .FirstOrDefaultAsync(cancellationToken);
            
        return Result<TagBriefDto?>.Success(tag);
    }
}
