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
