using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.Instructors.Commands.UpdateInstructorProfile;

public record UpdateInstructorProfileCommand : IRequest<Result<Guid>>
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? ImageUrl { get; init; }
    public string? Bio { get; init; }
    public string? Specialization { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Website { get; init; }
    public string? LinkedInProfile { get; init; }
    public int? YearsOfExperience { get; init; }
}

public class UpdateInstructorProfileCommandHandler : IRequestHandler<UpdateInstructorProfileCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public UpdateInstructorProfileCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(UpdateInstructorProfileCommand request, CancellationToken cancellationToken)
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

        var instructor = await _context.Instructors
            .FirstOrDefaultAsync(i => i.UserId == userId, cancellationToken);
            
        if (instructor == null)
        {
            return Result<Guid>.Failure(new[] { "Instructor not found" });
        }

        // Update user-level fields
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user != null)
        {
            if (request.FirstName != null) user.FirstName = request.FirstName;
            if (request.LastName != null) user.LastName = request.LastName;
            if (request.ImageUrl != null) user.ImageUrl = request.ImageUrl;
        }

        // Update instructor-specific fields
        if (request.Bio != null) instructor.Bio = request.Bio;
        if (request.Specialization != null) instructor.Specialization = request.Specialization;
        if (request.PhoneNumber != null) instructor.PhoneNumber = request.PhoneNumber;
        if (request.Website != null) instructor.Website = request.Website;
        if (request.LinkedInProfile != null) instructor.LinkedInProfile = request.LinkedInProfile;
        
        if (request.YearsOfExperience.HasValue)
        {
            instructor.YearsOfExperience = request.YearsOfExperience.Value;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(instructor.Id);
    }
}
