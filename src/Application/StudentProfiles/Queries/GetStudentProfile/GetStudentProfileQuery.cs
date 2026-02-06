using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.StudentProfiles.Queries.GetStudentProfile;

public record GetStudentProfileQuery : IRequest<Result<StudentProfileDto?>>
{
    public Guid? StudentId { get; init; }
    public Guid? UserId { get; init; }
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
        Guid studentId;
        
        if (request.StudentId.HasValue)
        {
            // Admin looking up by studentId
            studentId = request.StudentId.Value;
        }
        else if (request.UserId.HasValue)
        {
            // Admin looking up by userId
            var studentByUserId = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == request.UserId.Value, cancellationToken);
            
            if (studentByUserId == null)
            {
                return Result<StudentProfileDto?>.Failure(new[] { "Student not found" });
            }
            
            studentId = studentByUserId.Id;
        }
        else
        {
            // Current user looking up their own profile
            if (string.IsNullOrEmpty(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
            {
                return Result<StudentProfileDto?>.Failure(new[] { "Invalid user ID" });
            }

            // Get student by current user's ID
            var currentStudent = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
            
            if (currentStudent == null)
            {
                return Result<StudentProfileDto?>.Failure(new[] { "Student not found" });
            }
            
            studentId = currentStudent.Id;
        }

        // Get student by studentId
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.Id == studentId, cancellationToken);

        if (student == null)
        {
            return Result<StudentProfileDto?>.Failure(new[] { "Student not found" });
        }

        // Get user details
        var user = await _context.Users.FindAsync(new object[] { student.UserId }, cancellationToken);
        if (user == null)
        {
            return Result<StudentProfileDto?>.Failure(new[] { "User not found" });
        }

        // Get or create student profile
        var profile = await _context.StudentProfiles
            .FirstOrDefaultAsync(sp => sp.StudentId == studentId, cancellationToken);

        if (profile == null)
        {
            // Create a default profile
            profile = new Domain.Entities.StudentProfile
            {
                StudentId = studentId,
                IsMuslim = user.ReligionStatus == ReligiousStatus.BornMuslim || user.ReligionStatus == ReligiousStatus.RevertedMuslim,
                IsNewMuslim = user.ReligionStatus == ReligiousStatus.RevertedMuslim
            };
            _context.StudentProfiles.Add(profile);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Result<StudentProfileDto?>.Success(new StudentProfileDto
        {
            Id = profile.Id,
            StudentId = studentId,
            UserId = student.UserId,
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
