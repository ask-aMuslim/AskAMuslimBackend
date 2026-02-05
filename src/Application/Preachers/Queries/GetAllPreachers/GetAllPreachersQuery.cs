using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Mappings;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;
using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.Preachers.Queries.GetAllPreachers;

public record GetAllPreachersQuery : IRequest<Result<PaginatedList<PreacherDto>>>, IPaginationQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class GetAllPreachersQueryHandler : IRequestHandler<GetAllPreachersQuery, Result<PaginatedList<PreacherDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetAllPreachersQueryHandler(
        IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<PreacherDto>>> Handle(GetAllPreachersQuery request, CancellationToken cancellationToken)
    {
        var query = from p in _context.Preachers
                    join u in _context.Users on p.UserId equals u.Id into users
                    from u in users.DefaultIfEmpty()
                    where p.IsActive
                    select new PreacherDto
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
                        Certifications = p.Certifications,
                        Organization = p.Organization
                    };

        var paginatedList = await query
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .PaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);

        return Result<PaginatedList<PreacherDto>>.Success(paginatedList);
    }
}
