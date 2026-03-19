using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Options.Queries.GetOptionsByQuestion;

public record GetOptionsByQuestionQuery(Guid QuestionId) : IRequest<Result<List<OptionBriefDto>>>;

public class GetOptionsByQuestionQueryHandler : IRequestHandler<GetOptionsByQuestionQuery, Result<List<OptionBriefDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetOptionsByQuestionQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<OptionBriefDto>>> Handle(GetOptionsByQuestionQuery request, CancellationToken cancellationToken)
    {
        var data = await _context.Options
            .Where(o => o.QuestionId == request.QuestionId)
            .Select(s => new OptionBriefDto {
            Id = s.Id,
            Text = s.Text,
            IsCorrect = s.IsCorrect
    })
            .ToListAsync(cancellationToken);
        return Result<List<OptionBriefDto>>.Success(data);
    }
}
