using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.Enrollments.Queries.GetUnCompletedEnrollments;

public record GetUnCompletedEnrollmentsQuery(Guid StudentId) : IRequest<Result<List<EnrollmentDto>>>;

public class GetUnCompletedEnrollmentsQueryHandler : IRequestHandler<GetUnCompletedEnrollmentsQuery, Result<List<EnrollmentDto>>>
{
    private readonly IApplicationDbContext _context;
    public GetUnCompletedEnrollmentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<Result<List<EnrollmentDto>>> Handle(GetUnCompletedEnrollmentsQuery request, CancellationToken cancellationToken)
    {
        var enrollments = await _context.Enrollments
            .Where(e => e.StudentId == request.StudentId && e.Status == EnrollmentStatus.Active)
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


