using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.Students.Queries.GetStudentProfile;

public record GetStudentProfileQuery : IRequest<Result<StudentDto?>>
{
    public string? UserId { get; init; }
}

public class GetStudentProfileQueryHandler : IRequestHandler<GetStudentProfileQuery, Result<StudentDto?>>
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

    public async Task<Result<StudentDto?>> Handle(GetStudentProfileQuery request, CancellationToken cancellationToken)
    {
        var userIdString = request.UserId ?? _currentUser.Id;
        
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Result<StudentDto?>.Failure(new[] { "Invalid user ID" });
        }

        var student = await _context.Students
            .Include(s => s.Enrollments)
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        if (student == null)
        {
            return Result<StudentDto?>.Failure(new[] { "Student profile not found" });
        }

        // Get user details from Users table
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        var email = user?.Email ?? string.Empty;
        var firstName = user?.FirstName ?? string.Empty;
        var lastName = user?.LastName ?? string.Empty;

        // Check if profile exists
        var hasProfile = await _context.StudentProfiles
            .AnyAsync(sp => sp.StudentId == student.Id, cancellationToken);

        return Result<StudentDto?>.Success(new StudentDto
        {
            Id = student.Id,
            UserId = student.UserId,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            EnrollmentDate = student.EnrollmentDate,
            IsActive = student.IsActive,
            TotalEnrollments = student.Enrollments.Count,
            HasProfile = hasProfile
        });
    }
}
