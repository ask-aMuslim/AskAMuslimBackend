using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.AdminNotes.Queries.GetAdminNotes;

public record GetAdminNotesQuery : IRequest<Result<List<AdminNoteDto>>>
{
    public Guid StudentId { get; init; }
}

public class GetAdminNotesQueryHandler : IRequestHandler<GetAdminNotesQuery, Result<List<AdminNoteDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetAdminNotesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<AdminNoteDto>>> Handle(GetAdminNotesQuery request, CancellationToken cancellationToken)
    {
        var notes = await _context.AdminNotes
            .Where(an => an.StudentId == request.StudentId)
            .OrderByDescending(an => an.Created)
            .ToListAsync(cancellationToken);

        var result = new List<AdminNoteDto>();

        foreach (var note in notes)
        {
            var adminUser = await _context.Users.FindAsync(new object[] { note.AdminUserId }, cancellationToken);

            result.Add(new AdminNoteDto
            {
                Id = note.Id,
                StudentId = note.StudentId,
                AdminUserId = note.AdminUserId,
                AdminName = adminUser != null ? $"{adminUser.FirstName} {adminUser.LastName}" : "Unknown",
                Content = note.Content,
                IsPrivate = note.IsPrivate,
                CreatedAt = note.Created
            });
        }

        return Result<List<AdminNoteDto>>.Success(result);
    }
}
