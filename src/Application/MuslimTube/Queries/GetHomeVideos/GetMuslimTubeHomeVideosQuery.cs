using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Mappings;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;
using AskAMuslimBackend.Application.MuslimTube.Queries;
using AskAMuslimBackend.Application.MuslimTube.Queries.Caching;
using AutoMapper;
using AutoMapper.QueryableExtensions;


namespace AskAMuslimBackend.Application.MuslimTube.Queries.GetHomeVideos;

public record GetMuslimTubeHomeVideosQuery : IRequest<PaginatedList<MuslimTubeVideoBriefDto>>, IPaginationQuery
{
    public string? Search { get; init; }
    public bool Randomize { get; init; } = false;
    public DateTimeOffset? PublishedAfter { get; init; }
    public DateTimeOffset? PublishedBefore { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class GetMuslimTubeHomeVideosQueryHandler : IRequestHandler<GetMuslimTubeHomeVideosQuery, PaginatedList<MuslimTubeVideoBriefDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IMuslimTubeCache _cache;

    public GetMuslimTubeHomeVideosQueryHandler(IApplicationDbContext context, IMapper mapper, IMuslimTubeCache cache)
    {
        _context = context;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<PaginatedList<MuslimTubeVideoBriefDto>> Handle(GetMuslimTubeHomeVideosQuery request, CancellationToken cancellationToken)
    {
        var key = MuslimTubeCacheKeys.HomeVideos(request.PageNumber, request.PageSize, request.Search, request.Randomize, request.PublishedAfter, request.PublishedBefore);
        var cached = await _cache.GetVideosAsync(key, cancellationToken);
        if (cached != null) return cached;

        var query = _context.MuslimTubeVideos
            .Where(v => v.Channel != null && !v.Channel.IsDeleted);

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

        IOrderedQueryable<Domain.Entities.MuslimTubeVideo> ordered;
        if (request.Randomize)
        {
            // SQL Server translates OrderBy(Guid.NewGuid()) to ORDER BY NEWID()
            ordered = query.OrderBy(_ => Guid.NewGuid());
        }
        else
        {
            ordered = query.OrderByDescending(v => v.PublishedAt).ThenByDescending(v => v.Id);
        }

        var result = await ordered
            .ProjectTo<MuslimTubeVideoBriefDto>(_mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);

        await _cache.SetVideosAsync(key, result, cancellationToken);
        return result;
    }
}
