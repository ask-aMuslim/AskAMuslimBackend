using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;
using AskAMuslimBackend.Application.Common.Mappings;
using AskAMuslimBackend.Application.MuslimTube.Queries;
using AskAMuslimBackend.Application.MuslimTube.Queries.Caching;
using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace AskAMuslimBackend.Application.MuslimTube.Queries.GetChannelsWithPagination;

public record GetMuslimTubeChannelsWithPaginationQuery : IRequest<Result<PaginatedList<MuslimTubeChannelBriefDto>>>, IPaginationQuery
{
    public int? PageNumber { get; init; } = 1;
    public int? PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
}

public class GetMuslimTubeChannelsWithPaginationQueryHandler : IRequestHandler<GetMuslimTubeChannelsWithPaginationQuery, Result<PaginatedList<MuslimTubeChannelBriefDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IMuslimTubeCache _cache;

    public GetMuslimTubeChannelsWithPaginationQueryHandler(IApplicationDbContext context, IMapper mapper, IMuslimTubeCache cache)
    {
        _context = context;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<Result<PaginatedList<MuslimTubeChannelBriefDto>>> Handle(GetMuslimTubeChannelsWithPaginationQuery request, CancellationToken cancellationToken)
    {
        var key = MuslimTubeCacheKeys.Channels(request.PageNumber ?? 1, request.PageSize ?? 10);
        var cached = await _cache.GetChannelsAsync(key, cancellationToken);
        if (cached != null) 
            return Result<PaginatedList<MuslimTubeChannelBriefDto>>.Success(cached);

        var query = _context.MuslimTubeChannels.Where(c => !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(c => 
                c.Name.Contains(request.SearchTerm) ||
                (c.Description != null && c.Description.Contains(request.SearchTerm)));
        }

        var result = await query
            .OrderBy(c => c.Name)
            .ProjectTo<MuslimTubeChannelBriefDto>(_mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber ?? 1, request.PageSize ?? 10, cancellationToken);

        await _cache.SetChannelsAsync(key, result, cancellationToken);
        return Result<PaginatedList<MuslimTubeChannelBriefDto>>.Success(result);
    }
}
