using AskAMuslimBackend.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Instructors.Queries.GetAllInstructors;

public record GetAllInstructorsQuery : IRequest<List<InstructorDto>>;

public class GetAllInstructorsQueryHandler : IRequestHandler<GetAllInstructorsQuery, List<InstructorDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAllInstructorsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<InstructorDto>> Handle(GetAllInstructorsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Instructors
            .Include(i => i.Courses)
            .Where(i => i.IsActive)
            .Select(i => new InstructorDto
            {
                Id = i.Id,
                UserId = i.UserId,
                Email = string.Empty,
                FirstName = string.Empty,
                LastName = string.Empty,
                Bio = i.Bio,
                Specialization = i.Specialization,
                PhoneNumber = i.PhoneNumber,
                Website = i.Website,
                LinkedInProfile = i.LinkedInProfile,
                YearsOfExperience = i.YearsOfExperience,
                IsVerified = i.IsVerified,
                IsActive = i.IsActive,
                JoinedDate = i.JoinedDate,
                Rating = i.Rating,
                TotalReviews = i.TotalReviews,
                TotalCourses = i.Courses.Count
            })
            .ToListAsync(cancellationToken);
    }
}
