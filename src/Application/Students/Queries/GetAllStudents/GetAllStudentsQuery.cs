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
        var students = await _context.Students
            .Include(s => s.Enrollments)
            .Select(s => new StudentDto
            {
                Id = s.Id,
                UserId = s.UserId,
                Email = string.Empty, // Will be populated from Identity if needed
                FirstName = string.Empty,
                LastName = string.Empty,
                EnrollmentDate = s.EnrollmentDate,
                IsActive = s.IsActive,
                TotalEnrollments = s.Enrollments.Count,
                HasProfile = _context.StudentProfiles.Any(sp => sp.StudentId == s.Id)
            })
            .ToListAsync(cancellationToken);
            
        return Result<List<StudentDto>>.Success(students);
    }
}
