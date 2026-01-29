using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.Admins.Queries.GetAdminProfile;

public record GetAdminProfileQuery : IRequest<Result<AdminDto>>
{
    public string? UserId { get; init; }
}

public class GetAdminProfileQueryHandler : IRequestHandler<GetAdminProfileQuery, Result<AdminDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public GetAdminProfileQueryHandler(
        IApplicationDbContext context,
        IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<AdminDto>> Handle(GetAdminProfileQuery request, CancellationToken cancellationToken)
    {
        var userIdString = request.UserId ?? _currentUser.Id;
        
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Result<AdminDto>.Failure(new[] { "Invalid user ID" });
        }

        var admin = await _context.Admins
            .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);

        if (admin == null)
        {
            return Result<AdminDto>.Failure(new[] { "Admin not found" });
        }

        // Get user details from Users table
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        var email = user?.Email ?? string.Empty;
        var firstName = user?.FirstName ?? string.Empty;
        var lastName = user?.LastName ?? string.Empty;

        return Result<AdminDto>.Success(new AdminDto
        {
            Id = admin.Id,
            UserId = admin.UserId,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Department = admin.Department,
            PhoneNumber = admin.PhoneNumber,
            Bio = admin.Bio,
            ImageUrl = admin.ImageUrl,
            IsSuperAdmin = admin.IsSuperAdmin,
            IsActive = admin.IsActive,
            JoinedDate = admin.JoinedDate,
            LastLoginDate = admin.LastLoginDate
        });
    }
}
