using AskAMuslimBackend.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Students.Queries.GetAllStudents;

public record GetAllStudentsQuery : IRequest<List<StudentDto>>;

public class GetAllStudentsQueryHandler : IRequestHandler<GetAllStudentsQuery, List<StudentDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAllStudentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<StudentDto>> Handle(GetAllStudentsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Students
            .Include(s => s.Enrollments)
            .Select(s => new StudentDto
            {
                Id = s.Id,
                UserId = s.UserId,
                Email = string.Empty, // Will be populated from Identity if needed
                FirstName = string.Empty,
                LastName = string.Empty,
                Bio = s.Bio,
                PhoneNumber = s.PhoneNumber,
                Address = s.Address,
                DateOfBirth = s.DateOfBirth,
                EnrollmentDate = s.EnrollmentDate,
                IsActive = s.IsActive,
                TotalEnrollments = s.Enrollments.Count
            })
            .ToListAsync(cancellationToken);
    }
}
