using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.AdminNotes.Commands.CreateAdminNote;

public record CreateAdminNoteCommand : IRequest<Guid>
{
    public Guid StudentId { get; init; }
    public string Content { get; init; } = string.Empty;
    public bool IsPrivate { get; init; } = false;
}

public class CreateAdminNoteCommandHandler : IRequestHandler<CreateAdminNoteCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public CreateAdminNoteCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateAdminNoteCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUser.Id, out var adminUserId))
        {
            throw new UnauthorizedAccessException("Invalid user context");
        }

        var adminNote = new AdminNote
        {
            StudentId = request.StudentId,
            AdminUserId = adminUserId,
            Content = request.Content,
            IsPrivate = request.IsPrivate
        };

        _context.AdminNotes.Add(adminNote);
        await _context.SaveChangesAsync(cancellationToken);

        return adminNote.Id;
    }
}
