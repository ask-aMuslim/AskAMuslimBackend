using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.QuizAttempts.Queries.GetQuizAttemptsByQuiz;

public record GetQuizAttemptsByQuizQuery(Guid QuizId) : IRequest<Result<List<QuizAttemptDto>>>;

public class GetQuizAttemptsByQuizQueryHandler : IRequestHandler<GetQuizAttemptsByQuizQuery, Result<List<QuizAttemptDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetQuizAttemptsByQuizQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<List<QuizAttemptDto>>> Handle(GetQuizAttemptsByQuizQuery request, CancellationToken cancellationToken)
    {
        var attempts = await _context.QuizAttempts
            .Where(a => a.QuizId == request.QuizId)
            .OrderByDescending(a => a.StartedAt)
            .ProjectTo<QuizAttemptDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
        return Result<List<QuizAttemptDto>>.Success(attempts);
    }
}
