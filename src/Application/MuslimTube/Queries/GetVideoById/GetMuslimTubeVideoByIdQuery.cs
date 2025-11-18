using AskAMuslimBackend.Application.Common.Interfaces;

namespace AskAMuslimBackend.Application.MuslimTube.Queries.GetVideoById;

public record GetMuslimTubeVideoByIdQuery(Guid Id) : IRequest<MuslimTubeVideoBriefDto?>;

public class GetMuslimTubeVideoByIdQueryHandler : IRequestHandler<GetMuslimTubeVideoByIdQuery, MuslimTubeVideoBriefDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetMuslimTubeVideoByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<MuslimTubeVideoBriefDto?> Handle(GetMuslimTubeVideoByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.MuslimTubeVideos
            .Where(v => v.Id == request.Id)
            .ProjectTo<MuslimTubeVideoBriefDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
