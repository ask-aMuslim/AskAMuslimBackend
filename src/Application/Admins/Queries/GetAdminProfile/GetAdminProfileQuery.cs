using AskAMuslimBackend.Application.Common.Interfaces;

namespace AskAMuslimBackend.Application.Admins.Queries.GetAdminProfile;

public record GetAdminProfileQuery : IRequest<AdminDto?>
{
    public string? UserId { get; init; }
}

public class GetAdminProfileQueryHandler : IRequestHandler<GetAdminProfileQuery, AdminDto?>
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

    public async Task<AdminDto?> Handle(GetAdminProfileQuery request, CancellationToken cancellationToken)
    {
        var userIdString = request.UserId ?? _currentUser.Id;
        
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return null;
        }

        var admin = await _context.Admins
            .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);

        if (admin == null)
        {
            return null;
        }

        // Get user details from Users table
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        var email = user?.Email ?? string.Empty;
        var firstName = user?.FirstName ?? string.Empty;
        var lastName = user?.LastName ?? string.Empty;

        return new AdminDto
        {
            Id = admin.Id,
            UserId = admin.UserId,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Department = admin.Department,
            PhoneNumber = admin.PhoneNumber,
            IsSuperAdmin = admin.IsSuperAdmin,
            IsActive = admin.IsActive,
            JoinedDate = admin.JoinedDate,
            LastLoginDate = admin.LastLoginDate,
            CanManageUsers = admin.CanManageUsers,
            CanManageCourses = admin.CanManageCourses,
            CanManageContent = admin.CanManageContent,
            CanViewReports = admin.CanViewReports,
            CanManageRoles = admin.CanManageRoles
        };
    }
}
