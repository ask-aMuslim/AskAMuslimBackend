using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Levels.Queries.GetLevelById;

public record GetLevelByIdQuery(Guid Id) : IRequest<Result<LevelBriefDto?>>;

public class GetLevelByIdQueryHandler : IRequestHandler<GetLevelByIdQuery, Result<LevelBriefDto?>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetLevelByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<LevelBriefDto?>> Handle(GetLevelByIdQuery request, CancellationToken cancellationToken)
    {
        var level = await _context.Levels
            .Where(l => l.Id == request.Id)
            .ProjectTo<LevelBriefDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
            
        return Result<LevelBriefDto?>.Success(level);
    }
}
