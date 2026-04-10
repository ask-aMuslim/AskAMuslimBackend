using AskAMuslimBackend.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Progress.Queries.GetCourseProgress;

public record GetCourseProgressQuery(Guid CourseId) : IRequest<CourseProgressDto>;

public record CourseProgressDto(
    int CompletedLessonsCount,
    int TotalLessonsCount,
    double ProgressPercentage,
    bool IsCompleted
);

public class GetCourseProgressQueryHandler : IRequestHandler<GetCourseProgressQuery, CourseProgressDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetCourseProgressQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<CourseProgressDto> Handle(GetCourseProgressQuery request, CancellationToken cancellationToken)
    {
        if (_user.Id == null)
            throw new UnauthorizedAccessException();

        var studentId = Guid.Parse(_user.Id);

        var enrollment = await _context.Enrollments
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == request.CourseId, cancellationToken);

        if (enrollment == null)
        {
            return new CourseProgressDto(0, 0, 0, false);
        }

        var isCompleted = enrollment.CompletedLessonsCount >= enrollment.TotalLessonsCount && enrollment.TotalLessonsCount > 0;

        return new CourseProgressDto(
            enrollment.CompletedLessonsCount,
            enrollment.TotalLessonsCount,
            enrollment.CourseProgress,
            isCompleted
        );
    }
}