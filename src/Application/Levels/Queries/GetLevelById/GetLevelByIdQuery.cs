using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Levels.Queries.GetLevelById;

public record GetLevelByIdQuery(Guid Id) : IRequest<Result<LevelBriefDto?>>;

public class GetLevelByIdQueryHandler : IRequestHandler<GetLevelByIdQuery, Result<LevelBriefDto?>>
{
    private readonly IApplicationDbContext _context;

    public GetLevelByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<LevelBriefDto?>> Handle(GetLevelByIdQuery request, CancellationToken cancellationToken)
    {
        var level = await _context.Levels
            .Where(l => l.Id == request.Id)
            .Select(s => new LevelBriefDto {
            Id = s.Id,
            Title = s.Title,
            Description = s.Description,
            Order = s.Order,
            Difficulty = s.Difficulty,
            IsPublished = s.IsPublished,
            Language = s.Language,
            TotalCourses = s.Courses.Count
    })
            .FirstOrDefaultAsync(cancellationToken);
            
        return Result<LevelBriefDto?>.Success(level);
    }
}
