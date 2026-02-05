using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Mappings;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;
using AskAMuslimBackend.Application.MuslimTube.Queries;
using AskAMuslimBackend.Application.MuslimTube.Queries.Caching;
using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace AskAMuslimBackend.Application.MuslimTube.Queries.GetVideosByChannel;

public record GetMuslimTubeVideosByChannelQuery : IRequest<Result<PaginatedList<MuslimTubeVideoBriefDto>>>, IPaginationQuery
{
    public Guid ChannelId { get; init; }
    public string? Search { get; init; } // title/description contains
    public DateTimeOffset? PublishedAfter { get; init; }
    public DateTimeOffset? PublishedBefore { get; init; }
    public int? PageNumber { get; init; } = 1;
    public int? PageSize { get; init; } = 10;
}

public class GetMuslimTubeVideosByChannelQueryHandler : IRequestHandler<GetMuslimTubeVideosByChannelQuery, Result<PaginatedList<MuslimTubeVideoBriefDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IMuslimTubeCache _cache;

    public GetMuslimTubeVideosByChannelQueryHandler(IApplicationDbContext context, IMapper mapper, IMuslimTubeCache cache)
    {
        _context = context;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<Result<PaginatedList<MuslimTubeVideoBriefDto>>> Handle(GetMuslimTubeVideosByChannelQuery request, CancellationToken cancellationToken)
    {
        var key = MuslimTubeCacheKeys.Videos(request.ChannelId, request.PageNumber ?? 1, request.PageSize ?? 10, request.Search, request.PublishedAfter, request.PublishedBefore);
        var cached = await _cache.GetVideosAsync(key, cancellationToken);
        if (cached != null) 
            return Result<PaginatedList<MuslimTubeVideoBriefDto>>.Success(cached);

        var query = _context.MuslimTubeVideos.Where(v => v.MuslimTubeChannelId == request.ChannelId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(v => v.Title.Contains(term) || (v.Description != null && v.Description.Contains(term)));
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
            .ProjectTo<MuslimTubeVideoBriefDto>(_mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber ?? 1, request.PageSize ?? 10, cancellationToken);

        await _cache.SetVideosAsync(key, result, cancellationToken);
        return Result<PaginatedList<MuslimTubeVideoBriefDto>>.Success(result);
    }
}
