using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Mappings;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;

namespace AskAMuslimBackend.Application.Questions.Queries.GetQuestionsWithPagination;

public record GetQuestionsWithPaginationQuery : IRequest<Result<PaginatedList<QuestionBriefDto>>>, IPaginationQuery
{
    public Guid QuizId { get; init; }
    public int? PageNumber { get; init; } = 1;
    public int? PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
}

public class GetQuestionsWithPaginationQueryHandler : IRequestHandler<GetQuestionsWithPaginationQuery, Result<PaginatedList<QuestionBriefDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetQuestionsWithPaginationQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<QuestionBriefDto>>> Handle(GetQuestionsWithPaginationQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Questions.Where(q => q.QuizId == request.QuizId);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(q => q.Text.Contains(request.SearchTerm));
        }

        var paginatedList = await query
            .OrderBy(q => q.Order)
            .ThenBy(q => q.Text)
            .Select(s => new QuestionBriefDto {
            Id = s.Id,
            Text = s.Text,
            Order = s.Order,
            OptionsCount = s.Options.Count
    })
            .PaginatedListAsync(request.PageNumber ?? 1, request.PageSize ?? 10, cancellationToken);
        
        return Result<PaginatedList<QuestionBriefDto>>.Success(paginatedList);
    }
}
