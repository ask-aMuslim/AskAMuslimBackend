using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.QAs.Queries.GetQAsWithPagination;

public record GetQAsWithPaginationQuery : IRequest<Result<PaginatedList<QABriefDto>>>, IPaginationQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public bool? IsPublished { get; init; }
    public Language? Language { get; init; }
}

public class GetQAsWithPaginationQueryHandler : IRequestHandler<GetQAsWithPaginationQuery, Result<PaginatedList<QABriefDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetQAsWithPaginationQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<PaginatedList<QABriefDto>>> Handle(GetQAsWithPaginationQuery request, CancellationToken cancellationToken)
    {
        var query = _context.QAs.AsNoTracking();

        if (request.IsPublished.HasValue)
        {
            query = query.Where(q => q.IsPublished == request.IsPublished.Value);
        }

        var projected = query
            .OrderBy(q => q.Id)
            .Select(q => new QABriefDto
            {
                Id = q.Id,
                QuestionText = request.Language.HasValue
                    ? q.Translations
                        .Where(t => t.Language == request.Language.Value)
                        .Select(t => t.QuestionText)
                        .FirstOrDefault() ?? string.Empty
                    : q.Translations
                        .Select(t => t.QuestionText)
                        .FirstOrDefault() ?? string.Empty,
                AnswerText = request.Language.HasValue
                    ? q.Translations
                        .Where(t => t.Language == request.Language.Value)
                        .Select(t => t.AnswerText)
                        .FirstOrDefault() ?? string.Empty
                    : q.Translations
                        .Select(t => t.AnswerText)
                        .FirstOrDefault() ?? string.Empty,
                AnswerTextJson = request.Language.HasValue
                    ? q.Translations
                        .Where(t => t.Language == request.Language.Value)
                        .Select(t => t.AnswerTextJson)
                        .FirstOrDefault()
                    : q.Translations
                        .Select(t => t.AnswerTextJson)
                        .FirstOrDefault(),
                Tags = q.QATags
                    .Select(ct => ct.Tag != null ? ct.Tag.Title : "")
                    .Where(title => title != "")
                    .ToList()
            });

        var paginatedList = await PaginatedList<QABriefDto>.CreateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
        return Result<PaginatedList<QABriefDto>>.Success(paginatedList);
    }
}

public class GetQAsWithPaginationQueryValidator : PaginationQueryValidator<GetQAsWithPaginationQuery>
{
    public GetQAsWithPaginationQueryValidator()
    {
        RuleFor(x => x.Language)
            .IsInEnum()
            .When(x => x.Language.HasValue);
    }
}
