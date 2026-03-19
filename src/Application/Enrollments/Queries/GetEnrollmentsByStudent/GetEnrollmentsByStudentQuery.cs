using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Enrollments.Queries.GetEnrollmentsByStudent;

public record GetEnrollmentsByStudentQuery(Guid StudentId) : IRequest<Result<List<EnrollmentDto>>>;

public class GetEnrollmentsByStudentQueryHandler : IRequestHandler<GetEnrollmentsByStudentQuery, Result<List<EnrollmentDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetEnrollmentsByStudentQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<EnrollmentDto>>> Handle(GetEnrollmentsByStudentQuery request, CancellationToken cancellationToken)
    {
        var enrollments = await _context.Enrollments
            .Where(e => e.StudentId == request.StudentId)
            .OrderByDescending(e => e.EnrolledAt)
            .Select(s => new EnrollmentDto {
            Id = s.Id,
            StudentId = s.StudentId,
            CourseId = s.CourseId,
            EnrolledAt = s.EnrolledAt,
            Status = s.Status,
            LevelProgress = s.LevelProgress,
            CourseProgress = s.CourseProgress
    })
            .ToListAsync(cancellationToken);
            
        return Result<List<EnrollmentDto>>.Success(enrollments);
    }
}
