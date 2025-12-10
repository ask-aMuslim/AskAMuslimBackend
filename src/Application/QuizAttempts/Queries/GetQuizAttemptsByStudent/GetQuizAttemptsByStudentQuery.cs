using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.QuizAttempts.Queries.GetQuizAttemptsByStudent;

public record GetQuizAttemptsByStudentQuery(Guid StudentId) : IRequest<Result<List<QuizAttemptDto>>>;

public class GetQuizAttemptsByStudentQueryHandler : IRequestHandler<GetQuizAttemptsByStudentQuery, Result<List<QuizAttemptDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetQuizAttemptsByStudentQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<List<QuizAttemptDto>>> Handle(GetQuizAttemptsByStudentQuery request, CancellationToken cancellationToken)
    {
        var attempts = await _context.QuizAttempts
            .Where(a => a.StudentId == request.StudentId)
            .OrderByDescending(a => a.StartedAt)
            .ProjectTo<QuizAttemptDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
        return Result<List<QuizAttemptDto>>.Success(attempts);
    }
}
