using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.StudentProfiles.Commands.UpdateStudentProfile;

public record UpdateStudentProfileCommand : IRequest<Result<Guid>>
{
    // User-level fields
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? ImageUrl { get; init; }
    
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

public class UpdateStudentProfileCommandHandler : IRequestHandler<UpdateStudentProfileCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public UpdateStudentProfileCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(UpdateStudentProfileCommand request, CancellationToken cancellationToken)
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

        // Update user-level fields
        if (request.FirstName != null) user.FirstName = request.FirstName;
        if (request.LastName != null) user.LastName = request.LastName;
        if (request.ImageUrl != null) user.ImageUrl = request.ImageUrl;

        // Get or create student profile
        var profile = await _context.StudentProfiles
            .FirstOrDefaultAsync(sp => sp.StudentId == student.Id, cancellationToken);

        if (profile == null)
        {
            profile = new Domain.Entities.StudentProfile
            {
                StudentId = student.Id,
                IsMuslim = user.ReligionStatus == ReligiousStatus.BornMuslim || user.ReligionStatus == ReligiousStatus.RevertedMuslim,
                IsNewMuslim = user.ReligionStatus == ReligiousStatus.RevertedMuslim
            };
            _context.StudentProfiles.Add(profile);
        }

        // Update basic profile information
        if (request.Address != null) profile.Address = request.Address;
        if (request.Country != null) profile.Country = request.Country;
        if (request.CountryCode != null) profile.CountryCode = request.CountryCode;
        if (request.PhoneNumber != null) profile.PhoneNumber = request.PhoneNumber;
        if (request.Bio != null) profile.Bio = request.Bio;
        if (request.DateOfBirth.HasValue) profile.DateOfBirth = request.DateOfBirth;

        // Update religious information based on user's ReligionStatus
        profile.IsMuslim = user.ReligionStatus == ReligiousStatus.BornMuslim || user.ReligionStatus == ReligiousStatus.RevertedMuslim;
        profile.IsNewMuslim = user.ReligionStatus == ReligiousStatus.RevertedMuslim;

        // Only update revert-specific fields if user is a reverted Muslim
        if (user.ReligionStatus == ReligiousStatus.RevertedMuslim)
        {
            profile.DateOfIslamConversion = request.DateOfIslamConversion;
            profile.OldReligion = request.OldReligion;
            profile.ReasonForConversion = request.ReasonForConversion;
        }
        else
        {
            // Clear revert-specific fields if not a revert
            profile.DateOfIslamConversion = null;
            profile.OldReligion = null;
            profile.ReasonForConversion = null;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(profile.Id);
    }
}
