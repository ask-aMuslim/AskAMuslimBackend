using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.Students.Queries.GetStudentDashboard;

public record GetStudentDashboardQuery : IRequest<Result<PaginatedList<StudentDashboardDto>>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
    public string? Country { get; init; }
}

public class GetStudentDashboardQueryHandler : IRequestHandler<GetStudentDashboardQuery, Result<PaginatedList<StudentDashboardDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetStudentDashboardQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<StudentDashboardDto>>> Handle(GetStudentDashboardQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Students
            .Where(s => s.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchLower = request.SearchTerm.ToLower();
            query = query.Where(s => 
                _context.Users.Any(u => u.Id == s.UserId && 
                    (u.FirstName.ToLower().Contains(searchLower) || 
                     u.LastName.ToLower().Contains(searchLower) ||
                     (u.Email != null && u.Email.ToLower().Contains(searchLower)))));
        }

        if (!string.IsNullOrWhiteSpace(request.Country))
        {
            query = query.Where(s => s.Country == request.Country);
        }

        var students = await query
            .OrderBy(s => s.EnrollmentDate)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var studentDashboards = new List<StudentDashboardDto>();

        foreach (var student in students)
        {
            var user = await _context.Users.FindAsync(new object[] { student.UserId }, cancellationToken);
            
            var enrollments = await _context.Enrollments
                .Where(e => e.StudentId == student.Id)
                .Include(e => e.Course)
                .ThenInclude(c => c!.Level)
                .ToListAsync(cancellationToken);

            var quizAttempts = await _context.QuizAttempts
                .Where(qa => qa.StudentId == student.Id)
                .ToListAsync(cancellationToken);

            var currentEnrollment = enrollments
                .Where(e => e.Status == Domain.Enums.EnrollmentStatus.Active)
                .OrderByDescending(e => e.EnrolledAt)
                .FirstOrDefault();

            string? currentLesson = null;
            if (currentEnrollment != null)
            {
                var lastLesson = await _context.Lessons
                    .Where(l => l.CourseId == currentEnrollment.CourseId)
                    .OrderBy(l => l.Order)
                    .FirstOrDefaultAsync(cancellationToken);
                currentLesson = lastLesson?.Title;
            }

            studentDashboards.Add(new StudentDashboardDto
            {
                Id = student.Id,
                UserId = student.UserId,
                FirstName = user?.FirstName ?? string.Empty,
                LastName = user?.LastName ?? string.Empty,
                Email = user?.Email ?? string.Empty,
                Country = student.Country,
                IsActive = student.IsActive,
                Progress = new StudentProgressDto
                {
                    CurrentLevel = currentEnrollment?.Course?.Level?.Title,
                    CurrentCourse = currentEnrollment?.Course?.Title,
                    CurrentLesson = currentLesson,
                    AverageExamMarks = quizAttempts.Any() ? quizAttempts.Average(qa => qa.Score) : 0,
                    CompletedCourses = enrollments.Count(e => e.Status == Domain.Enums.EnrollmentStatus.Completed),
                    TotalEnrollments = enrollments.Count,
                    OverallProgress = enrollments.Any() ? (decimal)enrollments.Average(e => e.CourseProgress) : 0
                }
            });
        }

        var totalCount = await query.CountAsync(cancellationToken);

        return Result<PaginatedList<StudentDashboardDto>>.Success(
            new PaginatedList<StudentDashboardDto>(studentDashboards, totalCount, request.PageNumber, request.PageSize));
    }
}
