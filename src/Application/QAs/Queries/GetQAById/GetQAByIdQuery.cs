using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.QAs.Queries.GetQAById;

public record GetQAByIdQuery(Guid Id) : IRequest<Result<QADetailDto?>>;

public class GetQAByIdQueryHandler : IRequestHandler<GetQAByIdQuery, Result<QADetailDto?>>
{
    private readonly IApplicationDbContext _context;

    public GetQAByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<QADetailDto?>> Handle(GetQAByIdQuery request, CancellationToken cancellationToken)
    {
        var qa = await _context.QAs
            .AsNoTracking()
            .Where(q => q.Id == request.Id)
            .Select(s => new QADetailDto {
            Id = s.Id,
            IsPublished = s.IsPublished,
            Translations = s.Translations.Select(t => new QATranslationDto { Id = t.Id, Language = t.Language, QuestionText = t.QuestionText, AnswerText = t.AnswerText, AnswerTextJson = t.AnswerTextJson }).ToList(),
            TagIds = s.QATags.Select(t => t.TagId).ToList()
    })
            .FirstOrDefaultAsync(cancellationToken);
        
        return Result<QADetailDto?>.Success(qa);
    }
}
