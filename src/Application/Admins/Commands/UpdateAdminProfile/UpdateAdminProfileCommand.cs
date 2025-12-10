using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.Admins.Commands.UpdateAdminProfile;

public record UpdateAdminProfileCommand : IRequest<Result<Guid>>
{
    public string? Department { get; init; }
    public string? PhoneNumber { get; init; }
}

public class UpdateAdminProfileCommandHandler : IRequestHandler<UpdateAdminProfileCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public UpdateAdminProfileCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(UpdateAdminProfileCommand request, CancellationToken cancellationToken)
    {
        var userIdString = _currentUser.Id;
        
        if (string.IsNullOrEmpty(userIdString))
        {
            return Result<Guid>.Failure(new[] { "User not authenticated" });
        }

        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result<Guid>.Failure(new[] { "Invalid user ID format" });
        }

        var admin = await _context.Admins
            .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);
            
        if (admin == null)
        {
            return Result<Guid>.Failure(new[] { "Admin not found" });
        }

        admin.Department = request.Department;
        admin.PhoneNumber = request.PhoneNumber;
        admin.LastLoginDate = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(admin.Id);
    }
}
