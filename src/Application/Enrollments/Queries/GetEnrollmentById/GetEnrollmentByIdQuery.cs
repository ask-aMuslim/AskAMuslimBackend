using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Enrollments.Queries.GetEnrollmentById;

public record GetEnrollmentByIdQuery(Guid Id) : IRequest<Result<EnrollmentDto?>>;

public class GetEnrollmentByIdQueryHandler : IRequestHandler<GetEnrollmentByIdQuery, Result<EnrollmentDto?>>
{
    private readonly IApplicationDbContext _context;

    public GetEnrollmentByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<EnrollmentDto?>> Handle(GetEnrollmentByIdQuery request, CancellationToken cancellationToken)
    {
        var enrollment = await _context.Enrollments
            .AsNoTracking()
            .Where(e => e.Id == request.Id)
            .Select(e => new
            {
                e.Id,
                e.StudentId,
                e.CourseId,
                e.EnrolledAt,
                e.Status,
                e.LevelProgress,
                TotalLessonsCount = e.Course != null
                    ? e.Course.Lessons.Count(l => l.IsPublished)
                    : e.TotalLessonsCount
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (enrollment is null)
            return Result<EnrollmentDto?>.Success(null);

        var completedLessonsCount = await _context.StudentProgresses
            .AsNoTracking()
            .Where(sp =>
                sp.StudentId == enrollment.StudentId &&
                sp.CourseId == enrollment.CourseId &&
                sp.IsCompleted)
            .Join(
                _context.Lessons
                    .AsNoTracking()
                    .Where(l => l.CourseId == enrollment.CourseId && l.IsPublished),
                sp => sp.LessonId,
                l => l.Id,
                (sp, l) => sp.Id)
            .CountAsync(cancellationToken);

        var dto = new EnrollmentDto
        {
            Id = enrollment.Id,
            StudentId = enrollment.StudentId,
            CourseId = enrollment.CourseId,
            EnrolledAt = enrollment.EnrolledAt,
            Status = enrollment.Status,
            LevelProgress = enrollment.LevelProgress,
            CompletedLessonsCount = completedLessonsCount,
            LessonsInCourse = enrollment.TotalLessonsCount,
            RemainingLessonsCount = Math.Max(0, enrollment.TotalLessonsCount - completedLessonsCount),
            CourseProgress = enrollment.TotalLessonsCount == 0
                ? 0
                : Math.Round((double)completedLessonsCount / enrollment.TotalLessonsCount * 100, 2)
        };

        return Result<EnrollmentDto?>.Success(dto);
    }
}
