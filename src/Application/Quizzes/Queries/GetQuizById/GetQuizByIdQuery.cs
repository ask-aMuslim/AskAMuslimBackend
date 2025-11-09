using AskAMuslimBackend.Application.Common.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Quizzes.Queries.GetQuizById;

public record GetQuizByIdQuery(Guid Id) : IRequest<QuizBriefDto?>;

public class GetQuizByIdQueryHandler : IRequestHandler<GetQuizByIdQuery, QuizBriefDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetQuizByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<QuizBriefDto?> Handle(GetQuizByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.Quizzes
            .Where(q => q.Id == request.Id)
            .ProjectTo<QuizBriefDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
