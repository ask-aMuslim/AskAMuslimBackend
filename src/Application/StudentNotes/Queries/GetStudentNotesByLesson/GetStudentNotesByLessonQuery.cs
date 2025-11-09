using AskAMuslimBackend.Application.Common.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.StudentNotes.Queries.GetStudentNotesByLesson;

public record GetStudentNotesByLessonQuery(Guid LessonId) : IRequest<List<StudentNoteDto>>;

public class GetStudentNotesByLessonQueryHandler : IRequestHandler<GetStudentNotesByLessonQuery, List<StudentNoteDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetStudentNotesByLessonQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<StudentNoteDto>> Handle(GetStudentNotesByLessonQuery request, CancellationToken cancellationToken)
    {
        return await _context.StudentNotes
            .Where(n => n.LessonId == request.LessonId)
            .OrderBy(n => n.Timestamp)
            .ProjectTo<StudentNoteDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
    }
}
