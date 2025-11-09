using AskAMuslimBackend.Application.Common.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.StudentQuestions.Queries.GetStudentQuestionsByLesson;

public record GetStudentQuestionsByLessonQuery(Guid LessonId) : IRequest<List<StudentQuestionDto>>;

public class GetStudentQuestionsByLessonQueryHandler : IRequestHandler<GetStudentQuestionsByLessonQuery, List<StudentQuestionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetStudentQuestionsByLessonQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<StudentQuestionDto>> Handle(GetStudentQuestionsByLessonQuery request, CancellationToken cancellationToken)
    {
        return await _context.StudentQuestions
            .Where(q => q.LessonId == request.LessonId)
            .OrderBy(q => q.Timestamp)
            .ProjectTo<StudentQuestionDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
    }
}
