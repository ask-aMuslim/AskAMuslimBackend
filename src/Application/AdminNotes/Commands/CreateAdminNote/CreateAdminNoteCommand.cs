using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.AdminNotes.Commands.CreateAdminNote;

public record CreateAdminNoteCommand : IRequest<Result<Guid>>
{
    public Guid StudentId { get; init; }
    public string Content { get; init; } = string.Empty;
    public bool IsPrivate { get; init; } = false;
}

public class CreateAdminNoteCommandHandler : IRequestHandler<CreateAdminNoteCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    private readonly ILocalizationService _localizer;

    public CreateAdminNoteCommandHandler(IApplicationDbContext context, IUser currentUser, ILocalizationService localizer)
    {
        _context = context;
        _currentUser = currentUser;
        _localizer = localizer;
    }

    public async Task<Result<Guid>> Handle(CreateAdminNoteCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUser.Id, out var adminUserId))
        {
            return Result<Guid>.Failure(new[] { _localizer["Auth.InvalidUserContext"] });
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

        return Result<Guid>.Success(adminNote.Id);
    }
}
