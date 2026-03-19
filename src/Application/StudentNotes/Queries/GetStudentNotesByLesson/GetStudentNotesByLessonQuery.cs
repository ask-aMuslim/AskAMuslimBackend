using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.StudentNotes.Queries.GetStudentNotesByLesson;

public record GetStudentNotesByLessonQuery(Guid LessonId) : IRequest<Result<List<StudentNoteDto>>>;

public class GetStudentNotesByLessonQueryHandler : IRequestHandler<GetStudentNotesByLessonQuery, Result<List<StudentNoteDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetStudentNotesByLessonQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<StudentNoteDto>>> Handle(GetStudentNotesByLessonQuery request, CancellationToken cancellationToken)
    {
        var notes = await _context.StudentNotes
            .Where(n => n.LessonId == request.LessonId)
            .OrderBy(n => n.Timestamp)
            .Select(s => new StudentNoteDto {
            Id = s.Id,
            StudentId = s.StudentId,
            LessonId = s.LessonId,
            Text = s.Text,
            Timestamp = s.Timestamp
    })
            .ToListAsync(cancellationToken);
        
        return Result<List<StudentNoteDto>>.Success(notes);
    }
}
