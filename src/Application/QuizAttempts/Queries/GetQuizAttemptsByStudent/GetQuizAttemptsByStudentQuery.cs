using AskAMuslimBackend.Application.Common.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.QuizAttempts.Queries.GetQuizAttemptsByStudent;

public record GetQuizAttemptsByStudentQuery(Guid StudentId) : IRequest<List<QuizAttemptDto>>;

public class GetQuizAttemptsByStudentQueryHandler : IRequestHandler<GetQuizAttemptsByStudentQuery, List<QuizAttemptDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetQuizAttemptsByStudentQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<QuizAttemptDto>> Handle(GetQuizAttemptsByStudentQuery request, CancellationToken cancellationToken)
    {
        return await _context.QuizAttempts
            .Where(a => a.StudentId == request.StudentId)
            .OrderByDescending(a => a.StartedAt)
            .ProjectTo<QuizAttemptDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
    }
}
