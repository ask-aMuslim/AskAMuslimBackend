using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Instructors.Queries.GetAllInstructors;

public record GetAllInstructorsQuery : IRequest<Result<List<InstructorDto>>>;

public class GetAllInstructorsQueryHandler : IRequestHandler<GetAllInstructorsQuery, Result<List<InstructorDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetAllInstructorsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<InstructorDto>>> Handle(GetAllInstructorsQuery request, CancellationToken cancellationToken)
    {
        var instructors = await (from i in _context.Instructors.Include(i => i.Courses)
                                 join u in _context.Users on i.UserId equals u.Id into users
                                 from u in users.DefaultIfEmpty()
                                 where i.IsActive
                                 select new InstructorDto
                                 {
                                     Id = i.Id,
                                     UserId = i.UserId,
                                     Email = u != null ? u.Email ?? string.Empty : string.Empty,
                                     FirstName = u != null ? u.FirstName ?? string.Empty : string.Empty,
                                     LastName = u != null ? u.LastName ?? string.Empty : string.Empty,
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

        return Result<List<InstructorDto>>.Success(instructors);
    }
}
