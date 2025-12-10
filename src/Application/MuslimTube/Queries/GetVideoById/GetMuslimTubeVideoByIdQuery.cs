using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.MuslimTube.Queries.GetVideoById;

public record GetMuslimTubeVideoByIdQuery(Guid Id) : IRequest<Result<MuslimTubeVideoBriefDto?>>;

public class GetMuslimTubeVideoByIdQueryHandler : IRequestHandler<GetMuslimTubeVideoByIdQuery, Result<MuslimTubeVideoBriefDto?>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetMuslimTubeVideoByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<MuslimTubeVideoBriefDto?>> Handle(GetMuslimTubeVideoByIdQuery request, CancellationToken cancellationToken)
    {
        var video = await _context.MuslimTubeVideos
            .Where(v => v.Id == request.Id)
            .ProjectTo<MuslimTubeVideoBriefDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
        
        return Result<MuslimTubeVideoBriefDto?>.Success(video);
    }
}
