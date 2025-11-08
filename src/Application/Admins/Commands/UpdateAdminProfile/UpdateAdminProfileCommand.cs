using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.Admins.Commands.UpdateAdminProfile;

public record UpdateAdminProfileCommand : IRequest<Guid>
{
    public string? Department { get; init; }
    public string? PhoneNumber { get; init; }
}

public class UpdateAdminProfileCommandHandler : IRequestHandler<UpdateAdminProfileCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public UpdateAdminProfileCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(UpdateAdminProfileCommand request, CancellationToken cancellationToken)
    {
        var userIdString = _currentUser.Id ?? throw new UnauthorizedAccessException("User not authenticated");

        if (!Guid.TryParse(userIdString, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID format");
        }

        var admin = await _context.Admins
            .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(Admin), userId.ToString());

        admin.Department = request.Department;
        admin.PhoneNumber = request.PhoneNumber;
        admin.LastLoginDate = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return admin.Id;
    }
}
