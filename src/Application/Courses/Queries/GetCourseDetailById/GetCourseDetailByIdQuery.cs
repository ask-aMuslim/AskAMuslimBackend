using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;
namespace AskAMuslimBackend.Application.Courses.Queries.GetCourseDetailById;

public record GetCourseDetailByIdQuery(Guid CourseId, Guid? StudentId = null) : IRequest<Result<CourseDetailDto?>>;

public class GetCourseDetailByIdQueryHandler : IRequestHandler<GetCourseDetailByIdQuery, Result<CourseDetailDto?>>
{
    private readonly IApplicationDbContext _context;

    public GetCourseDetailByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<CourseDetailDto?>> Handle(GetCourseDetailByIdQuery request, CancellationToken cancellationToken)
    {
        var course = await _context.Courses
            .Where(c => c.Id == request.CourseId)
            .Select(s => new CourseDetailDto {
            Id = s.Id,
            Title = s.Title,
            Description = s.Description,
            ThumbnailUrl = s.ThumbnailUrl,
            IsPublished = s.IsPublished,
            Rating = s.Rating,
            Order = s.Order,
            LevelId = s.LevelId,
            Prerequisites = s.CoursePrerequisites.OrderBy(cp => cp.Order).Select(cp => new PrerequisiteCourseDto { Id = cp.PrerequisiteCourseId, Title = cp.PrerequisiteCourse!.Title, Order = cp.Order }).ToList(),
            UnlockedCourses = s.PrerequisiteForCourses.Select(cp => new PrerequisiteCourseDto { Id = cp.CourseId, Title = cp.Course!.Title, Order = cp.Order }).ToList()
    })
            .FirstOrDefaultAsync(cancellationToken);

        if (course == null)
        {
            return Result<CourseDetailDto?>.Success(null);
        }

        // If a student ID is provided, check which prerequisites are completed
        if (request.StudentId.HasValue)
        {
            var completedCourseIds = await _context.Enrollments
                .Where(e => e.StudentId == request.StudentId.Value && 
                           e.Status == EnrollmentStatus.Completed)
                .Select(e => e.CourseId)
                .ToListAsync(cancellationToken);

            foreach (var prerequisite in course.Prerequisites)
            {
                prerequisite.IsCompleted = completedCourseIds.Contains(prerequisite.Id);
            }

            foreach (var unlockedCourse in course.UnlockedCourses)
            {
                unlockedCourse.IsCompleted = completedCourseIds.Contains(unlockedCourse.Id);
            }
        }

        return Result<CourseDetailDto?>.Success(course);
    }
}
