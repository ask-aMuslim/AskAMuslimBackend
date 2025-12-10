using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Quizzes.Queries.GetQuizById;

public record GetQuizByIdQuery(Guid Id) : IRequest<Result<QuizBriefDto?>>;

public class GetQuizByIdQueryHandler : IRequestHandler<GetQuizByIdQuery, Result<QuizBriefDto?>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetQuizByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<QuizBriefDto?>> Handle(GetQuizByIdQuery request, CancellationToken cancellationToken)
    {
        var quiz = await _context.Quizzes
            .Where(q => q.Id == request.Id)
            .ProjectTo<QuizBriefDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
        return Result<QuizBriefDto?>.Success(quiz);
    }
}
