using AskAMuslimBackend.Application.Common.Interfaces;

namespace AskAMuslimBackend.Application.Instructors.Queries.GetInstructorProfile;

public record GetInstructorProfileQuery : IRequest<InstructorDto?>
{
    public string? UserId { get; init; }
}

public class GetInstructorProfileQueryHandler : IRequestHandler<GetInstructorProfileQuery, InstructorDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public GetInstructorProfileQueryHandler(
        IApplicationDbContext context,
        IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<InstructorDto?> Handle(GetInstructorProfileQuery request, CancellationToken cancellationToken)
    {
        var userId = request.UserId ?? _currentUser.Id;

        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        var instructor = await _context.Instructors
            .Include(i => i.Courses)
            .FirstOrDefaultAsync(i => i.UserId.ToString() == userId, cancellationToken);

        if (instructor == null)
        {
            return null;
        }

        return new InstructorDto
        {
            Id = instructor.Id,
            UserId = instructor.UserId,
            Email = string.Empty, // Will be populated from Identity if needed
            FirstName = string.Empty,
            LastName = string.Empty,
            Bio = instructor.Bio,
            Specialization = instructor.Specialization,
            PhoneNumber = instructor.PhoneNumber,
            Website = instructor.Website,
            LinkedInProfile = instructor.LinkedInProfile,
            YearsOfExperience = instructor.YearsOfExperience,
            IsVerified = instructor.IsVerified,
            IsActive = instructor.IsActive,
            JoinedDate = instructor.JoinedDate,
            Rating = instructor.Rating,
            TotalReviews = instructor.TotalReviews,
            TotalCourses = instructor.Courses.Count
        };
    }
}
