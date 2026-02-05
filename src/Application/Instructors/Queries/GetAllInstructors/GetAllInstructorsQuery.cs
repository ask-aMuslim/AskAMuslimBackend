using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Mappings;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Instructors.Queries.GetAllInstructors;

public record GetAllInstructorsQuery : IRequest<Result<PaginatedList<InstructorDto>>>, IPaginationQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class GetAllInstructorsQueryHandler : IRequestHandler<GetAllInstructorsQuery, Result<PaginatedList<InstructorDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetAllInstructorsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<InstructorDto>>> Handle(GetAllInstructorsQuery request, CancellationToken cancellationToken)
    {
        var query = from i in _context.Instructors.Include(i => i.Courses)
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
                        ImageUrl = u != null ? u.ImageUrl ?? i.ImageUrl : i.ImageUrl,
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
                    };

        var paginatedList = await query
            .OrderBy(i => i.LastName)
            .ThenBy(i => i.FirstName)
            .PaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);

        return Result<PaginatedList<InstructorDto>>.Success(paginatedList);
    }
}
