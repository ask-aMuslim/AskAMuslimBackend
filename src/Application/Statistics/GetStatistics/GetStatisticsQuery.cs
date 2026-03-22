using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Statistics.GetStatistics;

public record GetStatisticsQuery : IRequest<Result<StatisticsDto>>;

public class GetStatisticsQueryHandler : IRequestHandler<GetStatisticsQuery, Result<StatisticsDto>>
{
    private readonly IApplicationDbContext _context;

    public GetStatisticsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<StatisticsDto>> Handle(GetStatisticsQuery request, CancellationToken cancellationToken)
    {
        // Get total students count
        var totalStudents = await _context.Students.CountAsync(cancellationToken);

        // Get total Muslims (students who have profile and IsMuslim = true)
        var totalMuslims = await _context.StudentProfiles
            .Where(sp => sp.IsMuslim)
            .CountAsync(cancellationToken);

        // Get total New Muslims (students who have profile and IsNewMuslim = true)
        var totalNewMuslims = await _context.StudentProfiles
            .Where(sp => sp.IsNewMuslim)
            .CountAsync(cancellationToken);

        // Get total courses
        var totalCourses = await _context.Courses.CountAsync(cancellationToken);

        // Get total lessons
        var totalLessons = await _context.Lessons.CountAsync(cancellationToken);

        // Get total completed courses (enrollments with Status = Completed)
        var totalCompletedCourses = await _context.Enrollments
            .Where(e => e.Status == Domain.Enums.EnrollmentStatus.Completed)
            .CountAsync(cancellationToken);

        // Get total volunteers
        var totalVolunteers = await _context.Volunteers.CountAsync(cancellationToken);

        var statistics = new StatisticsDto
        {
            TotalStudents = totalStudents,
            TotalMuslims = totalMuslims,
            TotalNewMuslims = totalNewMuslims,
            TotalCourses = totalCourses,
            TotalLessons = totalLessons,
            TotalCompletedCourses = totalCompletedCourses,
            TotalVolunteers = totalVolunteers
        };

        return Result<StatisticsDto>.Success(statistics);
    }
}
