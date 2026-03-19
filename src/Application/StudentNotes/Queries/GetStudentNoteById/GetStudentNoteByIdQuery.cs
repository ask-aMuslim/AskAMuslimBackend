using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.StudentNotes.Queries.GetStudentNoteById;

public record GetStudentNoteByIdQuery(Guid Id) : IRequest<Result<StudentNoteDto?>>;

public class GetStudentNoteByIdQueryHandler : IRequestHandler<GetStudentNoteByIdQuery, Result<StudentNoteDto?>>
{
    private readonly IApplicationDbContext _context;

    public GetStudentNoteByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<StudentNoteDto?>> Handle(GetStudentNoteByIdQuery request, CancellationToken cancellationToken)
    {
        var note = await _context.StudentNotes
            .Where(n => n.Id == request.Id)
            .Select(s => new StudentNoteDto {
            Id = s.Id,
            StudentId = s.StudentId,
            LessonId = s.LessonId,
            Text = s.Text,
            Timestamp = s.Timestamp
    })
            .FirstOrDefaultAsync(cancellationToken);
        
        return Result<StudentNoteDto?>.Success(note);
    }
}
