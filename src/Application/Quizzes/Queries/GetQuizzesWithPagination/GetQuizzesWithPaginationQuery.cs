using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Mappings;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.Quizzes.Queries.GetQuizzesWithPagination;

public record GetQuizzesWithPaginationQuery : IRequest<PaginatedList<QuizBriefDto>>
{
    public Guid? TargetId { get; init; }
    public QuizTargetType? TargetType { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class GetQuizzesWithPaginationQueryHandler : IRequestHandler<GetQuizzesWithPaginationQuery, PaginatedList<QuizBriefDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetQuizzesWithPaginationQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PaginatedList<QuizBriefDto>> Handle(GetQuizzesWithPaginationQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Quizzes.AsQueryable();

        if (request.TargetId.HasValue)
        {
            query = query.Where(q => q.TargetId == request.TargetId.Value);
        }
        if (request.TargetType.HasValue)
        {
            query = query.Where(q => q.TargetType == request.TargetType.Value);
        }

        return await query
            .OrderBy(q => q.Title)
            .ProjectTo<QuizBriefDto>(_mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);
    }
}
