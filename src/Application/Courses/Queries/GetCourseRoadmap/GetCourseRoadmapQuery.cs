using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.Courses.Queries.GetCourseRoadmap;

public record GetCourseRoadmapQuery(Guid LevelId, Guid? StudentId = null) : IRequest<Result<CourseRoadmapDto>>;

public class GetCourseRoadmapQueryHandler : IRequestHandler<GetCourseRoadmapQuery, Result<CourseRoadmapDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public GetCourseRoadmapQueryHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<CourseRoadmapDto>> Handle(GetCourseRoadmapQuery request, CancellationToken cancellationToken)
    {
        // Check if level exists
        var level = await _context.Levels
            .Where(l => l.Id == request.LevelId)
            .Select(l => new { l.Id, l.Title })
            .FirstOrDefaultAsync(cancellationToken);

        if (level == null)
        {
            return Result<CourseRoadmapDto>.Failure(new[] { "Level not found." });
        }

        // Get all courses in the level
        var courses = await _context.Courses
            .Where(c => c.LevelId == request.LevelId)
            .Select(c => new RoadmapCourseDto
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                ThumbnailUrl = c.ThumbnailUrl,
                IsPublished = c.IsPublished,
                Order = c.Order
            })
            .ToListAsync(cancellationToken);

        // Get all prerequisites for courses in this level
        var courseIds = courses.Select(c => c.Id).ToList();
        var prerequisites = await _context.CoursePrerequisites
            .Where(cp => courseIds.Contains(cp.CourseId))
            .Select(cp => new
            {
                cp.CourseId,
                cp.PrerequisiteCourseId,
                cp.Order
            })
            .ToListAsync(cancellationToken);

        // Build prerequisite relationships
        foreach (var course in courses)
        {
            course.PrerequisiteIds = prerequisites
                .Where(p => p.CourseId == course.Id)
                .OrderBy(p => p.Order)
                .Select(p => p.PrerequisiteCourseId)
                .ToList();
        }

        // If student ID provided, get their enrollment status for each course
        // If not provided but the caller is an authenticated student, resolve via JWT
        var studentId = request.StudentId;
        if (!studentId.HasValue && Guid.TryParse(_currentUser.Id, out var currentUserId))
        {
            var student = await _context.Students
                .Where(s => s.UserId == currentUserId)
                .Select(s => new { s.Id })
                .FirstOrDefaultAsync(cancellationToken);

            if (student != null)
                studentId = student.Id;
        }

        if (studentId.HasValue)
        {
            // Include prerequisite course IDs that may belong to other levels
            var allPrerequisiteIds = prerequisites.Select(p => p.PrerequisiteCourseId).Distinct().ToList();
            var allRelevantCourseIds = courseIds.Union(allPrerequisiteIds).ToList();

            var enrollments = await _context.Enrollments
                .Where(e => e.StudentId == studentId.Value && 
                           allRelevantCourseIds.Contains(e.CourseId))
                .Select(e => new
                {
                    e.CourseId,
                    e.Status,
                    e.CourseProgress
                })
                .ToListAsync(cancellationToken);

            foreach (var course in courses)
            {
                var enrollment = enrollments.FirstOrDefault(e => e.CourseId == course.Id);
                if (enrollment != null)
                {
                    course.EnrollmentStatus = enrollment.Status;
                    course.Progress = enrollment.CourseProgress;
                }

                // Check if course is available (all prerequisites completed)
                if (course.PrerequisiteIds.Any())
                {
                    var completedPrerequisites = enrollments
                        .Where(e => course.PrerequisiteIds.Contains(e.CourseId) && 
                                   e.Status == EnrollmentStatus.Completed)
                        .Select(e => e.CourseId)
                        .ToList();

                    course.IsAvailable = course.PrerequisiteIds.All(p => completedPrerequisites.Contains(p));
                }
                else
                {
                    course.IsAvailable = true; // No prerequisites, always available
                }
            }
        }
        else
        {
            // No student context — courses without prerequisites are available if published;
            // courses with prerequisites are never available without a known student
            foreach (var course in courses)
            {
                course.IsAvailable = course.IsPublished && !course.PrerequisiteIds.Any();
            }
        }

        var roadmap = new CourseRoadmapDto
        {
            LevelId = level.Id,
            LevelTitle = level.Title,
            Courses = courses
        };

        return Result<CourseRoadmapDto>.Success(roadmap);
    }
}

public class CourseRoadmapDto
{
    public Guid LevelId { get; init; }
    public string LevelTitle { get; init; } = string.Empty;
    public List<RoadmapCourseDto> Courses { get; init; } = new();
}

public class RoadmapCourseDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ThumbnailUrl { get; init; }
    public bool IsPublished { get; init; }
    public int Order { get; init; }
    
    public List<Guid> PrerequisiteIds { get; set; } = new();
    public bool IsAvailable { get; set; }
    public EnrollmentStatus? EnrollmentStatus { get; set; }
    public double? Progress { get; set; }
}
