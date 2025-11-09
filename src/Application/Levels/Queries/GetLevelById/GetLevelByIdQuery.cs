using AskAMuslimBackend.Application.Common.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Levels.Queries.GetLevelById;

public record GetLevelByIdQuery(Guid Id) : IRequest<LevelBriefDto?>;

public class GetLevelByIdQueryHandler : IRequestHandler<GetLevelByIdQuery, LevelBriefDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetLevelByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<LevelBriefDto?> Handle(GetLevelByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.Levels
            .Where(l => l.Id == request.Id)
            .ProjectTo<LevelBriefDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
