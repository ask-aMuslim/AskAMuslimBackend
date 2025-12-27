using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.StudentProfiles.Commands.CreateStudentProfile;

public record CreateStudentProfileCommand : IRequest<Result<Guid>>
{
    // Basic Profile Information
    public string? Address { get; init; }
    public string? Country { get; init; }
    public string? CountryCode { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Bio { get; init; }
    public DateOnly? DateOfBirth { get; init; }

    // For New Muslims (Reverts) only
    public DateOnly? DateOfIslamConversion { get; init; }
    public string? OldReligion { get; init; }
    public string? ReasonForConversion { get; init; }
}

public class CreateStudentProfileCommandHandler : IRequestHandler<CreateStudentProfileCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public CreateStudentProfileCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(CreateStudentProfileCommand request, CancellationToken cancellationToken)
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

        // Get user to check religious status
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null)
        {
            return Result<Guid>.Failure(new[] { "User not found" });
        }

        // Get student
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        if (student == null)
        {
            return Result<Guid>.Failure(new[] { $"Student with user ID {userId} not found" });
        }

        // Check if profile already exists
        var existingProfile = await _context.StudentProfiles
            .FirstOrDefaultAsync(sp => sp.StudentId == student.Id, cancellationToken);

        if (existingProfile != null)
        {
            return Result<Guid>.Failure(new[] { "Student profile already exists. Use update instead." });
        }

        // Determine religious status flags from User entity
        var isMuslim = user.ReligionStatus == ReligiousStatus.BornMuslim || user.ReligionStatus == ReligiousStatus.RevertedMuslim;
        var isNewMuslim = user.ReligionStatus == ReligiousStatus.RevertedMuslim;

        // Create new profile
        var profile = new Domain.Entities.StudentProfile
        {
            StudentId = student.Id,
            Address = request.Address,
            Country = request.Country,
            CountryCode = request.CountryCode,
            PhoneNumber = request.PhoneNumber,
            Bio = request.Bio,
            DateOfBirth = request.DateOfBirth,
            IsMuslim = isMuslim,
            IsNewMuslim = isNewMuslim,
            // Only set revert-specific fields if user is a reverted Muslim
            DateOfIslamConversion = isNewMuslim ? request.DateOfIslamConversion : null,
            OldReligion = isNewMuslim ? request.OldReligion : null,
            ReasonForConversion = isNewMuslim ? request.ReasonForConversion : null
        };

        _context.StudentProfiles.Add(profile);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(profile.Id);
    }
}
