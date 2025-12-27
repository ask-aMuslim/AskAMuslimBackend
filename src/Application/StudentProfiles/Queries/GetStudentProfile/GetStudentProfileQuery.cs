using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.StudentProfiles.Queries.GetStudentProfile;

public record GetStudentProfileQuery : IRequest<Result<StudentProfileDto?>>
{
    public string? UserId { get; init; }
}

public class GetStudentProfileQueryHandler : IRequestHandler<GetStudentProfileQuery, Result<StudentProfileDto?>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public GetStudentProfileQueryHandler(
        IApplicationDbContext context,
        IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<StudentProfileDto?>> Handle(GetStudentProfileQuery request, CancellationToken cancellationToken)
    {
        var userIdString = request.UserId ?? _currentUser.Id;
        
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Result<StudentProfileDto?>.Failure(new[] { "Invalid user ID" });
        }

        // Get user details
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null)
        {
            return Result<StudentProfileDto?>.Failure(new[] { "User not found" });
        }

        // Get student
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        if (student == null)
        {
            return Result<StudentProfileDto?>.Failure(new[] { "Student not found" });
        }

        // Get or create student profile
        var profile = await _context.StudentProfiles
            .FirstOrDefaultAsync(sp => sp.StudentId == student.Id, cancellationToken);

        if (profile == null)
        {
            // Create a default profile
            profile = new Domain.Entities.StudentProfile
            {
                StudentId = student.Id,
                IsMuslim = user.ReligionStatus == ReligiousStatus.BornMuslim || user.ReligionStatus == ReligiousStatus.RevertedMuslim,
                IsNewMuslim = user.ReligionStatus == ReligiousStatus.RevertedMuslim
            };
            _context.StudentProfiles.Add(profile);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Result<StudentProfileDto?>.Success(new StudentProfileDto
        {
            Id = profile.Id,
            StudentId = student.Id,
            UserId = userId,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ReligiousStatus = user.ReligionStatus ?? ReligiousStatus.NonMuslim,
            Address = profile.Address,
            Country = profile.Country,
            CountryCode = profile.CountryCode,
            PhoneNumber = profile.PhoneNumber,
            Bio = profile.Bio,
            DateOfBirth = profile.DateOfBirth,
            IsMuslim = profile.IsMuslim,
            IsNewMuslim = profile.IsNewMuslim,
            DateOfIslamConversion = profile.DateOfIslamConversion,
            OldReligion = profile.OldReligion,
            ReasonForConversion = profile.ReasonForConversion
        });
    }
}
