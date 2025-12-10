using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.Instructors.Queries.GetInstructorProfile;

public record GetInstructorProfileQuery : IRequest<Result<InstructorDto>>
{
    public string? UserId { get; init; }
}

public class GetInstructorProfileQueryHandler : IRequestHandler<GetInstructorProfileQuery, Result<InstructorDto>>
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

    public async Task<Result<InstructorDto>> Handle(GetInstructorProfileQuery request, CancellationToken cancellationToken)
    {
        var userId = request.UserId ?? _currentUser.Id;

        if (string.IsNullOrEmpty(userId))
        {
            return Result<InstructorDto>.Failure(new[] { "Invalid user ID" });
        }

        var instructor = await _context.Instructors
            .Include(i => i.Courses)
            .FirstOrDefaultAsync(i => i.UserId.ToString() == userId, cancellationToken);

        if (instructor == null)
        {
            return Result<InstructorDto>.Failure(new[] { "Instructor not found" });
        }

        return Result<InstructorDto>.Success(new InstructorDto
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
        });
    }
}
