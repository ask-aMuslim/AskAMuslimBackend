using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.QAs.Queries.GetQAsWithPagination;

public record GetQAsWithPaginationQuery : IRequest<Result<PaginatedList<QABriefDto>>>, IPaginationQuery
{
    public int? PageNumber { get; init; } = 1;
    public int? PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
    public bool? IsPublished { get; init; }
    public Language? Language { get; init; }
    public List<Guid>? TagIds { get; init; }
}

public class GetQAsWithPaginationQueryHandler : IRequestHandler<GetQAsWithPaginationQuery, Result<PaginatedList<QABriefDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetQAsWithPaginationQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<QABriefDto>>> Handle(GetQAsWithPaginationQuery request, CancellationToken cancellationToken)
    {
        var query = _context.QAs.AsNoTracking();

        if (request.IsPublished.HasValue)
        {
            query = query.Where(q => q.IsPublished == request.IsPublished.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(q => q.Translations.Any(t => 
                t.QuestionText.Contains(request.SearchTerm) ||
                t.AnswerText.Contains(request.SearchTerm)));
        }

        if (request.TagIds != null && request.TagIds.Any())
        {
            query = query.Where(q => q.QATags.Any(qt => request.TagIds.Contains(qt.TagId)));
        }

        var projected = query
            .OrderBy(q => q.Id)
            .Select(q => new QABriefDto
            {
                Id = q.Id,
                IsPublished = q.IsPublished,
                Translations = !request.Language.HasValue
                    ? q.Translations
                        .Select(t => new QABriefTranslationDto
                        {
                            Language = t.Language,
                            QuestionText = t.QuestionText,
                            AnswerText = t.AnswerText,
                            AnswerTextJson = t.AnswerTextJson
                        })
                        .ToList()
                    : null,
                Tags = q.QATags
                    .Select(ct => ct.Tag != null ? ct.Tag.Title : "")
                    .Where(title => title != "")
                    .ToList()
            });

        var paginatedList = await PaginatedList<QABriefDto>.CreateAsync(projected, request.PageNumber ?? 1, request.PageSize ?? 10, cancellationToken);
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
