using AskAMuslimBackend.Application.Common.Interfaces;

namespace AskAMuslimBackend.Application.Students.Queries.GetStudentProfile;

public record GetStudentProfileQuery : IRequest<StudentDto?>
{
    public string? UserId { get; init; }
}

public class GetStudentProfileQueryHandler : IRequestHandler<GetStudentProfileQuery, StudentDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly IUser _currentUser;

    public GetStudentProfileQueryHandler(
        IApplicationDbContext context,
        IIdentityService identityService,
        IUser currentUser)
    {
        _context = context;
        _identityService = identityService;
        _currentUser = currentUser;
    }

    public async Task<StudentDto?> Handle(GetStudentProfileQuery request, CancellationToken cancellationToken)
    {
        var userIdString = request.UserId ?? _currentUser.Id;
        
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return null;
        }

        var student = await _context.Students
            .Include(s => s.Enrollments)
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        if (student == null)
        {
            return null;
        }

        // Get user details from Users table
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        var email = user?.Email ?? string.Empty;
        var firstName = user?.FirstName ?? string.Empty;
        var lastName = user?.LastName ?? string.Empty;

        return new StudentDto
        {
            Id = student.Id,
            UserId = student.UserId,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Bio = student.Bio,
            PhoneNumber = student.PhoneNumber,
            Address = student.Address,
            DateOfBirth = student.DateOfBirth,
            EnrollmentDate = student.EnrollmentDate,
            IsActive = student.IsActive,
            TotalEnrollments = student.Enrollments.Count
        };
    }
}
