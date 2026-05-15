using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Enrollments.Queries.GetEnrollmentsByCourse;

public record GetEnrollmentsByCourseQuery(Guid CourseId) : IRequest<Result<List<EnrollmentDto>>>;

public class GetEnrollmentsByCourseQueryHandler : IRequestHandler<GetEnrollmentsByCourseQuery, Result<List<EnrollmentDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetEnrollmentsByCourseQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<EnrollmentDto>>> Handle(GetEnrollmentsByCourseQuery request, CancellationToken cancellationToken)
    {
        var enrollments = await _context.Enrollments
            .Where(e => e.CourseId == request.CourseId)
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
                CourseProgress = s.TotalLessonsCount == 0
                    ? 0
                    : Math.Round(((double)s.CompletedLessonsCount / s.TotalLessonsCount) * 100, 2),
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
