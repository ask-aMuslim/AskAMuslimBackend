using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Students.Queries.GetAllStudents;

public record GetAllStudentsQuery : IRequest<Result<List<StudentDto>>>;

public class GetAllStudentsQueryHandler : IRequestHandler<GetAllStudentsQuery, Result<List<StudentDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetAllStudentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<StudentDto>>> Handle(GetAllStudentsQuery request, CancellationToken cancellationToken)
    {
        var students = await (from s in _context.Students.Include(s => s.Enrollments)
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
                              })
                              .ToListAsync(cancellationToken);

        return Result<List<StudentDto>>.Success(students);
    }
}
