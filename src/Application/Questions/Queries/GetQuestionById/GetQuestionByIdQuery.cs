using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Questions.Queries.GetQuestionById;

public record GetQuestionByIdQuery(Guid Id) : IRequest<Result<QuestionBriefDto?>>;

public class GetQuestionByIdQueryHandler : IRequestHandler<GetQuestionByIdQuery, Result<QuestionBriefDto?>>
{
    private readonly IApplicationDbContext _context;

    public GetQuestionByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<QuestionBriefDto?>> Handle(GetQuestionByIdQuery request, CancellationToken cancellationToken)
    {
        var data = await _context.Questions
            .Where(q => q.Id == request.Id)
            .Select(s => new QuestionBriefDto {
            Id = s.Id,
            Text = s.Text,
            Order = s.Order,
            OptionsCount = s.Options.Count
    })
            .FirstOrDefaultAsync(cancellationToken);
        return Result<QuestionBriefDto?>.Success(data);
    }
}
