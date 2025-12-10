using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.StudentQuestions.Queries.GetStudentQuestionsByLesson;

public record GetStudentQuestionsByLessonQuery(Guid LessonId) : IRequest<Result<List<StudentQuestionDto>>>;

public class GetStudentQuestionsByLessonQueryHandler : IRequestHandler<GetStudentQuestionsByLessonQuery, Result<List<StudentQuestionDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetStudentQuestionsByLessonQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<List<StudentQuestionDto>>> Handle(GetStudentQuestionsByLessonQuery request, CancellationToken cancellationToken)
    {
        var questions = await _context.StudentQuestions
            .Where(q => q.LessonId == request.LessonId)
            .OrderBy(q => q.Timestamp)
            .ProjectTo<StudentQuestionDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
        
        return Result<List<StudentQuestionDto>>.Success(questions);
    }
}
