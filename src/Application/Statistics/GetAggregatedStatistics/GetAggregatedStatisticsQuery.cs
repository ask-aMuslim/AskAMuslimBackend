using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Statistics.GetAggregatedStatistics;

public record GetAggregatedStatisticsQuery : IRequest<Result<AggregatedStatisticsDto>>;

public class GetAggregatedStatisticsQueryHandler : IRequestHandler<GetAggregatedStatisticsQuery, Result<AggregatedStatisticsDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAggregatedStatisticsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<AggregatedStatisticsDto>> Handle(GetAggregatedStatisticsQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var firstDayOfMonth = DateTime.SpecifyKind(new DateTime(now.Year, now.Month, 1), DateTimeKind.Utc);

        // User Statistics
        var totalStudents = await _context.Students.CountAsync(cancellationToken);
        var activeStudents = await _context.Students.CountAsync(s => s.IsActive, cancellationToken);
        var inactiveStudents = totalStudents - activeStudents;
        
        var totalAdmins = await _context.Admins.CountAsync(cancellationToken);
        var activeAdmins = await _context.Admins.CountAsync(a => a.IsActive, cancellationToken);
        
        var totalInstructors = await _context.Instructors.CountAsync(cancellationToken);
        var activeInstructors = await _context.Instructors.CountAsync(i => i.IsActive, cancellationToken);
        
        var totalPreachers = await _context.Preachers.CountAsync(cancellationToken);
        var activePreachers = await _context.Preachers.CountAsync(p => p.IsActive, cancellationToken);

        var totalUsers = totalStudents + totalAdmins + totalInstructors + totalPreachers;
        var activeUsers = activeStudents + activeAdmins + activeInstructors + activePreachers;
        var inactiveUsers = totalUsers - activeUsers;

        // Non-Muslim students
        var nonMuslimStudents = await _context.StudentProfiles
            .CountAsync(sp => !sp.IsMuslim, cancellationToken);

        // Volunteers
        var totalVolunteers = await _context.Volunteers.CountAsync(cancellationToken);

        // New users this month - using EnrollmentDate for students, JoinedDate for others
        var firstDayOfMonthDateOnly = DateOnly.FromDateTime(firstDayOfMonth);
        var newStudentsThisMonth = await _context.Students
            .CountAsync(s => s.EnrollmentDate >= firstDayOfMonthDateOnly, cancellationToken);
        var newAdminsThisMonth = await _context.Admins
            .CountAsync(a => a.JoinedDate >= firstDayOfMonth, cancellationToken);
        var newInstructorsThisMonth = await _context.Instructors
            .CountAsync(i => i.JoinedDate >= firstDayOfMonth, cancellationToken);
        var newPreachersThisMonth = await _context.Preachers
            .CountAsync(p => p.JoinedDate >= firstDayOfMonth, cancellationToken);
        var newUsersThisMonth = newStudentsThisMonth + newAdminsThisMonth + newInstructorsThisMonth + newPreachersThisMonth;

        // Last login statistics - daily data for last 30 days
        var dailyLoginData = new List<DailyLoginData>();
        for (int i = 29; i >= 0; i--)
        {
            var date = DateTime.SpecifyKind(today.AddDays(-i), DateTimeKind.Utc);
            var count = await _context.Users
                .CountAsync(u => u.LastLoginAt.HasValue && 
                                 u.LastLoginAt.Value.Date == date, 
                            cancellationToken);
            
            dailyLoginData.Add(new DailyLoginData
            {
                Date = date.ToString("yyyy-MM-dd"),
                Count = count
            });
        }

        // Course Statistics
        var totalCourses = await _context.Courses.CountAsync(cancellationToken);
        var publishedCourses = await _context.Courses.CountAsync(c => c.IsPublished, cancellationToken);

        // Courses by level
        var coursesByLevel = await _context.Courses
            .Include(c => c.Level)
            .GroupBy(c => c.Level!.Title)
            .Select(g => new { Level = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var byLevelDict = coursesByLevel.ToDictionary(x => x.Level, x => x.Count);

        // QA Translation Statistics
        var qaTranslations = await _context.QATranslations
            .GroupBy(q => q.Language)
            .Select(g => new { Language = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var qaStats = new QATranslationStatistics();
        foreach (var translation in qaTranslations)
        {
            switch (translation.Language)
            {
                case Language.English:
                    qaStats.English = translation.Count;
                    break;
                case Language.Arabic:
                    qaStats.Arabic = translation.Count;
                    break;
                case Language.French:
                    qaStats.French = translation.Count;
                    break;
                case Language.Spanish:
                    qaStats.Spanish = translation.Count;
                    break;
                case Language.German:
                    qaStats.German = translation.Count;
                    break;
                case Language.Portuguese:
                    qaStats.Portuguese = translation.Count;
                    break;
            }
        }

        var statistics = new AggregatedStatisticsDto
        {
            Users = new UserStatistics
            {
                Total = totalUsers,
                Active = activeUsers,
                Inactive = inactiveUsers,
                NewThisMonth = newUsersThisMonth,
                LastLogin = new LastLoginStatistics
                {
                    Daily = dailyLoginData
                },
                ByRole = new UserRoleStatistics
                {
                    Admins = totalAdmins,
                    Instructors = totalInstructors,
                    Preachers = totalPreachers,
                    Students = totalStudents,
                    NonMuslim = nonMuslimStudents,
                    Volunteers = totalVolunteers
                }
            },
            Courses = new CourseStatistics
            {
                Total = totalCourses,
                Published = publishedCourses,
                ByLevel = byLevelDict
            },
            QATranslation = qaStats
        };

        return Result<AggregatedStatisticsDto>.Success(statistics);
    }
}
