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
            .Where(e => e.Id == request.Id)
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
            .FirstOrDefaultAsync(cancellationToken);
            
        return Result<EnrollmentDto?>.Success(enrollment);
    }
}
