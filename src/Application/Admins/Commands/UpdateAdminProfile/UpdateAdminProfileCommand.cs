using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.Admins.Commands.UpdateAdminProfile;

public record UpdateAdminProfileCommand : IRequest<Result<Guid>>
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? ImageUrl { get; init; }
    public string? Bio { get; init; }
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

        // Update user-level fields
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user != null)
        {
            if (request.FirstName != null) user.FirstName = request.FirstName;
            if (request.LastName != null) user.LastName = request.LastName;
            if (request.ImageUrl != null) user.ImageUrl = request.ImageUrl;
        }

        // Update admin-specific fields
        if (request.Bio != null) admin.Bio = request.Bio;
        if (request.Department != null) admin.Department = request.Department;
        if (request.PhoneNumber != null) admin.PhoneNumber = request.PhoneNumber;
        admin.LastLoginDate = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(admin.Id);
    }
}
