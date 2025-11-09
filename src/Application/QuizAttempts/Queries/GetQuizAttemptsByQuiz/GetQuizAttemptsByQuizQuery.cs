using AskAMuslimBackend.Application.Common.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.QuizAttempts.Queries.GetQuizAttemptsByQuiz;

public record GetQuizAttemptsByQuizQuery(Guid QuizId) : IRequest<List<QuizAttemptDto>>;

public class GetQuizAttemptsByQuizQueryHandler : IRequestHandler<GetQuizAttemptsByQuizQuery, List<QuizAttemptDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetQuizAttemptsByQuizQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<QuizAttemptDto>> Handle(GetQuizAttemptsByQuizQuery request, CancellationToken cancellationToken)
    {
        return await _context.QuizAttempts
            .Where(a => a.QuizId == request.QuizId)
            .OrderByDescending(a => a.StartedAt)
            .ProjectTo<QuizAttemptDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
    }
}
