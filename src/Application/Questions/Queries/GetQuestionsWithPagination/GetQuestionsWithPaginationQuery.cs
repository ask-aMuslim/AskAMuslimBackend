using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Mappings;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;

namespace AskAMuslimBackend.Application.Questions.Queries.GetQuestionsWithPagination;

public record GetQuestionsWithPaginationQuery : IRequest<Result<PaginatedList<QuestionBriefDto>>>, IPaginationQuery
{
    public Guid QuizId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class GetQuestionsWithPaginationQueryHandler : IRequestHandler<GetQuestionsWithPaginationQuery, Result<PaginatedList<QuestionBriefDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetQuestionsWithPaginationQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<PaginatedList<QuestionBriefDto>>> Handle(GetQuestionsWithPaginationQuery request, CancellationToken cancellationToken)
    {
        var paginatedList = await _context.Questions
            .Where(q => q.QuizId == request.QuizId)
            .OrderBy(q => q.Text)
            .ProjectTo<QuestionBriefDto>(_mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);
        
        return Result<PaginatedList<QuestionBriefDto>>.Success(paginatedList);
    }
}
