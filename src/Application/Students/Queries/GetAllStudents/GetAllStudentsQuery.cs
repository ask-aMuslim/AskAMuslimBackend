using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Mappings;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Students.Queries.GetAllStudents;

public record GetAllStudentsQuery : IRequest<Result<PaginatedList<StudentDto>>>, IPaginationQuery
{
    public int? PageNumber { get; init; } = 1;
    public int? PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
}

public class GetAllStudentsQueryHandler : IRequestHandler<GetAllStudentsQuery, Result<PaginatedList<StudentDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetAllStudentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<StudentDto>>> Handle(GetAllStudentsQuery request, CancellationToken cancellationToken)
    {
        var query = from s in _context.Students.Include(s => s.Enrollments)
                    join u in _context.Users on s.UserId equals u.Id into users
                    from u in users.DefaultIfEmpty()
                    select new StudentDto
                    {
                        Id = s.Id,
                        UserId = s.UserId,
                        Email = u != null ? u.Email ?? string.Empty : string.Empty,
                        FirstName = u != null ? u.FirstName ?? string.Empty : string.Empty,
                        LastName = u != null ? u.LastName ?? string.Empty : string.Empty,
                        EnrollmentDate = s.EnrollmentDate,
                        IsActive = s.IsActive,
                        TotalEnrollments = s.Enrollments.Count,
                        HasProfile = _context.StudentProfiles.Any(sp => sp.StudentId == s.Id)
                    };

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(s => 
                s.Email.Contains(request.SearchTerm) ||
                s.FirstName.Contains(request.SearchTerm) ||
                s.LastName.Contains(request.SearchTerm));
        }

        var paginatedList = await query
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .PaginatedListAsync(request.PageNumber ?? 1, request.PageSize ?? 10, cancellationToken);

        return Result<PaginatedList<StudentDto>>.Success(paginatedList);
    }
}
