using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Mappings;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.QAs.Queries.GetQAsWithPagination;

public record GetQAsWithPaginationQuery : IRequest<PaginatedList<QABriefDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public bool? IsPublished { get; init; }
}

public class GetQAsWithPaginationQueryHandler : IRequestHandler<GetQAsWithPaginationQuery, PaginatedList<QABriefDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetQAsWithPaginationQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PaginatedList<QABriefDto>> Handle(GetQAsWithPaginationQuery request, CancellationToken cancellationToken)
    {
        var query = _context.QAs
            .AsNoTracking()
            .AsQueryable();

        if (request.IsPublished.HasValue)
        {
            query = query.Where(q => q.IsPublished == request.IsPublished.Value);
        }

        return await query
            .OrderBy(q => q.Id)
            .ProjectTo<QABriefDto>(_mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);
    }
}

public class GetQAsWithPaginationQueryValidator : AbstractValidator<GetQAsWithPaginationQuery>
{
    public GetQAsWithPaginationQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(1);
    }
}
