using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.MuslimTube.Queries.GetVideoById;

public record GetMuslimTubeVideoByIdQuery(Guid Id) : IRequest<Result<MuslimTubeVideoBriefDto?>>;

public class GetMuslimTubeVideoByIdQueryHandler : IRequestHandler<GetMuslimTubeVideoByIdQuery, Result<MuslimTubeVideoBriefDto?>>
{
    private readonly IApplicationDbContext _context;

    public GetMuslimTubeVideoByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<MuslimTubeVideoBriefDto?>> Handle(GetMuslimTubeVideoByIdQuery request, CancellationToken cancellationToken)
    {
        var video = await _context.MuslimTubeVideos
            .Where(v => v.Id == request.Id)
            .Select(s => new MuslimTubeVideoBriefDto {
            Id = s.Id,
            YouTubeVideoId = s.YouTubeVideoId,
            MuslimTubeChannelId = s.MuslimTubeChannelId,
            Title = s.Title,
            Description = s.Description,
            ThumbnailDefaultUrl = s.ThumbnailDefaultUrl,
            ThumbnailMediumUrl = s.ThumbnailMediumUrl,
            ThumbnailHighUrl = s.ThumbnailHighUrl,
            PublishedAt = s.PublishedAt,
            VideoUrl = s.VideoUrl
    })
            .FirstOrDefaultAsync(cancellationToken);
        
        return Result<MuslimTubeVideoBriefDto?>.Success(video);
    }
}
