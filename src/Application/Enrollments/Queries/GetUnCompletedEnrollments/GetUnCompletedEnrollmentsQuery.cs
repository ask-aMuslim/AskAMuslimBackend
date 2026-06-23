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
            .Where(e => e.StudentId == student.Id && e.Status == EnrollmentStatus.Active && e.Course != null && e.Course.IsPublished)
            .OrderByDescending(e => e.EnrolledAt)
            .Select(s => new
            {
                s.Id,
                s.StudentId,
                s.CourseId,
                s.EnrolledAt,
                s.Status,
                s.LevelProgress,
                TotalLessonsCount = s.Course != null ? s.Course.Lessons.Count(l => l.IsPublished) : s.TotalLessonsCount,
                CompletedLessonsCount = _context.StudentProgresses
                    .AsNoTracking()
                    .Where(sp => sp.StudentId == s.StudentId && sp.CourseId == s.CourseId && sp.IsCompleted)
                    .Join(
                        _context.Lessons.AsNoTracking().Where(l => l.CourseId == s.CourseId && l.IsPublished),
                        sp => sp.LessonId,
                        l => l.Id,
                        (sp, _) => sp.Id)
                    .Count()
            })
            .Select(s => new EnrollmentDto
            {
                Id = s.Id,
                StudentId = s.StudentId,
                CourseId = s.CourseId,
                EnrolledAt = s.EnrolledAt,
                Status = s.Status,
                LevelProgress = s.LevelProgress,
                CourseProgress = Math.Round(((double)s.CompletedLessonsCount / (s.TotalLessonsCount == 0 ? 1 : s.TotalLessonsCount)) * 100, 2),
                CompletedLessonsCount = s.CompletedLessonsCount,
                RemainingLessonsCount = (s.TotalLessonsCount - s.CompletedLessonsCount) > 0
                    ? (s.TotalLessonsCount - s.CompletedLessonsCount)
                    : 0,
                LessonsInCourse = s.TotalLessonsCount
            })
            .ToListAsync(cancellationToken);
            
        return Result<List<EnrollmentDto>>.Success(enrollments);
    }
}


