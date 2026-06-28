using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Mappings;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;
using AskAMuslimBackend.Application.MuslimTube.Queries;
using AskAMuslimBackend.Application.MuslimTube.Queries.Caching;
namespace AskAMuslimBackend.Application.MuslimTube.Queries.GetVideosByChannel;

public record GetMuslimTubeVideosByChannelQuery : IRequest<Result<PaginatedList<MuslimTubeVideoBriefDto>>>, IPaginationQuery
{
    public Guid ChannelId { get; init; }
    public string? SearchTerm { get; init; } // title/description contains
    public DateTimeOffset? PublishedAfter { get; init; }
    public DateTimeOffset? PublishedBefore { get; init; }
    public int? PageNumber { get; init; } = 1;
    public int? PageSize { get; init; } = 10;
}

public class GetMuslimTubeVideosByChannelQueryHandler : IRequestHandler<GetMuslimTubeVideosByChannelQuery, Result<PaginatedList<MuslimTubeVideoBriefDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMuslimTubeCache _cache;

    public GetMuslimTubeVideosByChannelQueryHandler(IApplicationDbContext context, IMuslimTubeCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<Result<PaginatedList<MuslimTubeVideoBriefDto>>> Handle(GetMuslimTubeVideosByChannelQuery request, CancellationToken cancellationToken)
    {
        var key = MuslimTubeCacheKeys.Videos(request.ChannelId, request.PageNumber ?? 1, request.PageSize ?? 10, request.SearchTerm, request.PublishedAfter, request.PublishedBefore);
        var cached = await _cache.GetVideosAsync(key, cancellationToken);
        if (cached != null) 
            return Result<PaginatedList<MuslimTubeVideoBriefDto>>.Success(cached);

        var query = _context.MuslimTubeVideos.Where(v => v.MuslimTubeChannelId == request.ChannelId);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(v => v.Title.ToLower().Contains(term) || (v.Description != null && v.Description.ToLower().Contains(term)));
        }
        if (request.PublishedAfter.HasValue)
        {
            query = query.Where(v => v.PublishedAt != null && v.PublishedAt >= request.PublishedAfter.Value);
        }
        if (request.PublishedBefore.HasValue)
        {
            query = query.Where(v => v.PublishedAt != null && v.PublishedAt <= request.PublishedBefore.Value);
        }

        var result = await query
            .OrderByDescending(v => v.PublishedAt)
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
            .PaginatedListAsync(request.PageNumber ?? 1, request.PageSize ?? 10, cancellationToken);

        await _cache.SetVideosAsync(key, result, cancellationToken);
        return Result<PaginatedList<MuslimTubeVideoBriefDto>>.Success(result);
    }
}
