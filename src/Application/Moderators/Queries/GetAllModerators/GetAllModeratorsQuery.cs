using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Mappings;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Moderators.Queries.GetAllModerators;

public record GetAllModeratorsQuery : IRequest<Result<PaginatedList<ModeratorDto>>>, IPaginationQuery
{
    public int? PageNumber { get; init; } = 1;
    public int? PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
    public UserRole? RoleFilter { get; init; } // Optional: filter by specific role
}

public class GetAllModeratorsQueryHandler : IRequestHandler<GetAllModeratorsQuery, Result<PaginatedList<ModeratorDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetAllModeratorsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<ModeratorDto>>> Handle(GetAllModeratorsQuery request, CancellationToken cancellationToken)
    {
        // Query for Admins
        var adminsQuery = from a in _context.Admins
                          join u in _context.Users on a.UserId equals u.Id into users
                          from u in users.DefaultIfEmpty()
                          where a.IsActive
                          select new ModeratorDto
                          {
                              Id = a.Id,
                              UserId = a.UserId,
                              Email = u != null ? u.Email ?? string.Empty : string.Empty,
                              FirstName = u != null ? u.FirstName ?? string.Empty : string.Empty,
                              LastName = u != null ? u.LastName ?? string.Empty : string.Empty,
                              Bio = a.Bio,
                              ImageUrl = u != null ? u.ImageUrl ?? a.ImageUrl : a.ImageUrl,
                              PhoneNumber = a.PhoneNumber,
                              IsActive = a.IsActive,
                              JoinedDate = a.JoinedDate,
                              Role = UserRole.Admin,
                              Department = a.Department,
                              IsSuperAdmin = a.IsSuperAdmin,
                              // Default values for non-admin fields
                              Specialization = null,
                              Website = null,
                              LinkedInProfile = null,
                              YearsOfExperience = 0,
                              IsVerified = true,
                              Rating = 0,
                              TotalReviews = 0,
                              Certifications = null,
                              Organization = null,
                              TotalCourses = 0
                          };

        // Query for Instructors
        var instructorsQuery = from i in _context.Instructors.Include(i => i.Courses)
                               join u in _context.Users on i.UserId equals u.Id into users
                               from u in users.DefaultIfEmpty()
                               where i.IsActive
                               select new ModeratorDto
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
                                   Role = UserRole.Instructor,
                                   TotalCourses = i.Courses.Count,
                                   // Default values for non-instructor fields
                                   Department = null,
                                   IsSuperAdmin = false,
                                   Certifications = null,
                                   Organization = null
                               };

        // Query for Preachers
        var preachersQuery = from p in _context.Preachers
                             join u in _context.Users on p.UserId equals u.Id into users
                             from u in users.DefaultIfEmpty()
                             where p.IsActive
                             select new ModeratorDto
                             {
                                 Id = p.Id,
                                 UserId = p.UserId,
                                 Email = u != null ? u.Email ?? string.Empty : string.Empty,
                                 FirstName = u != null ? u.FirstName ?? string.Empty : string.Empty,
                                 LastName = u != null ? u.LastName ?? string.Empty : string.Empty,
                                 Bio = p.Bio,
                                 ImageUrl = u != null ? u.ImageUrl ?? p.ImageUrl : p.ImageUrl,
                                 Specialization = p.Specialization,
                                 PhoneNumber = p.PhoneNumber,
                                 Website = p.Website,
                                 LinkedInProfile = p.LinkedInProfile,
                                 YearsOfExperience = p.YearsOfExperience,
                                 IsVerified = p.IsVerified,
                                 IsActive = p.IsActive,
                                 JoinedDate = p.JoinedDate,
                                 Rating = p.Rating,
                                 TotalReviews = p.TotalReviews,
                                 Role = UserRole.Preacher,
                                 Certifications = p.Certifications,
                                 Organization = p.Organization,
                                 // Default values for non-preacher fields
                                 Department = null,
                                 IsSuperAdmin = false,
                                 TotalCourses = 0
                             };

        // Combine all three queries
        IQueryable<ModeratorDto> query;
        
        if (request.RoleFilter.HasValue)
        {
            query = request.RoleFilter.Value switch
            {
                UserRole.Admin => adminsQuery,
                UserRole.Instructor => instructorsQuery,
                UserRole.Preacher => preachersQuery,
                _ => adminsQuery.Concat(instructorsQuery).Concat(preachersQuery)
            };
        }
        else
        {
            query = adminsQuery.Concat(instructorsQuery).Concat(preachersQuery);
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(m =>
                m.Email.Contains(request.SearchTerm) ||
                m.FirstName.Contains(request.SearchTerm) ||
                m.LastName.Contains(request.SearchTerm) ||
                (m.Specialization != null && m.Specialization.Contains(request.SearchTerm)) ||
                (m.Department != null && m.Department.Contains(request.SearchTerm)) ||
                (m.Organization != null && m.Organization.Contains(request.SearchTerm)));
        }

        // Materialize the query (since Concat doesn't support all LINQ operations)
        var allModerators = await query.ToListAsync(cancellationToken);

        // Apply sorting and pagination in memory
        var sortedModerators = allModerators
            .OrderBy(m => m.LastName)
            .ThenBy(m => m.FirstName)
            .ToList();

        var pageNumber = request.PageNumber ?? 1;
        var pageSize = request.PageSize ?? 10;

        var totalCount = sortedModerators.Count;
        var paginatedItems = sortedModerators
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var paginatedList = new PaginatedList<ModeratorDto>(
            paginatedItems,
            totalCount,
            pageNumber,
            pageSize);

        return Result<PaginatedList<ModeratorDto>>.Success(paginatedList);
    }
}
