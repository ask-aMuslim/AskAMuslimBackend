using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.Enrollments.Queries.GetUnCompletedEnrollments;

public record GetUnCompletedEnrollmentsQuery : IRequest<Result<List<EnrollmentDto>>>;

public class GetUnCompletedEnrollmentsQueryHandler : IRequestHandler<GetUnCompletedEnrollmentsQuery, Result<List<EnrollmentDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public GetUnCompletedEnrollmentsQueryHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }
    public async Task<Result<List<EnrollmentDto>>> Handle(GetUnCompletedEnrollmentsQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
        {
            return Result<List<EnrollmentDto>>.Failure(new[] { "Invalid user ID" });
        }

        var student = await _context.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        if (student == null)
        {
            return Result<List<EnrollmentDto>>.Failure(new[] { "Student not found" });
        }

        var enrollments = await _context.Enrollments
            .Where(e => e.StudentId == student.Id && e.Status == EnrollmentStatus.Active)
            .OrderByDescending(e => e.EnrolledAt)
            .Select(s => new EnrollmentDto {
            Id = s.Id,
            StudentId = s.StudentId,
            CourseId = s.CourseId,
            EnrolledAt = s.EnrolledAt,
            Status = s.Status,
            LevelProgress = s.LevelProgress,
            CourseProgress = s.CourseProgress,
            CompletedLessonsCount = s.CompletedLessonsCount,
                RemainingLessonsCount =
                    ((s.Course != null ? s.Course.Lessons.Count : s.TotalLessonsCount) - s.CompletedLessonsCount) > 0
                        ? (s.Course != null ? s.Course.Lessons.Count : s.TotalLessonsCount) - s.CompletedLessonsCount
                        : 0,
                LessonsInCourse = s.Course != null ? s.Course.Lessons.Count : s.TotalLessonsCount
    })
            .ToListAsync(cancellationToken);
            
        return Result<List<EnrollmentDto>>.Success(enrollments);
    }
}


