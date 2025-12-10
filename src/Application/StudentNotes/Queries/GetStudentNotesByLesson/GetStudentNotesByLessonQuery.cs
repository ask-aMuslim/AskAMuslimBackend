using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.StudentNotes.Queries.GetStudentNotesByLesson;

public record GetStudentNotesByLessonQuery(Guid LessonId) : IRequest<Result<List<StudentNoteDto>>>;

public class GetStudentNotesByLessonQueryHandler : IRequestHandler<GetStudentNotesByLessonQuery, Result<List<StudentNoteDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetStudentNotesByLessonQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<List<StudentNoteDto>>> Handle(GetStudentNotesByLessonQuery request, CancellationToken cancellationToken)
    {
        var notes = await _context.StudentNotes
            .Where(n => n.LessonId == request.LessonId)
            .OrderBy(n => n.Timestamp)
            .ProjectTo<StudentNoteDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
        
        return Result<List<StudentNoteDto>>.Success(notes);
    }
}
