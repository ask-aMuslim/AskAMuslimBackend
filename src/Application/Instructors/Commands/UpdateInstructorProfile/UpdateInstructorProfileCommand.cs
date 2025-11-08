using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.Instructors.Commands.UpdateInstructorProfile;

public record UpdateInstructorProfileCommand : IRequest<Guid>
{
    public string? Bio { get; init; }
    public string? Specialization { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Website { get; init; }
    public string? LinkedInProfile { get; init; }
    public int? YearsOfExperience { get; init; }
}

public class UpdateInstructorProfileCommandHandler : IRequestHandler<UpdateInstructorProfileCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public UpdateInstructorProfileCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(UpdateInstructorProfileCommand request, CancellationToken cancellationToken)
    {
        var userIdString = _currentUser.Id ?? throw new UnauthorizedAccessException("User not authenticated");

        if (!Guid.TryParse(userIdString, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID format");
        }

        var instructor = await _context.Instructors
            .FirstOrDefaultAsync(i => i.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(Instructor), userId.ToString());

        instructor.Bio = request.Bio;
        instructor.Specialization = request.Specialization;
        instructor.PhoneNumber = request.PhoneNumber;
        instructor.Website = request.Website;
        instructor.LinkedInProfile = request.LinkedInProfile;
        
        if (request.YearsOfExperience.HasValue)
        {
            instructor.YearsOfExperience = request.YearsOfExperience.Value;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return instructor.Id;
    }
}
