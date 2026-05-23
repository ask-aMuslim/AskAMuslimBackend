using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Courses.Queries.GetCourseById;

public record GetCourseByIdQuery(Guid Id) : IRequest<Result<CourseWithDetailsDto?>>;

public class GetCourseByIdQueryHandler : IRequestHandler<GetCourseByIdQuery, Result<CourseWithDetailsDto?>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public GetCourseByIdQueryHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<CourseWithDetailsDto?>> Handle(GetCourseByIdQuery request, CancellationToken cancellationToken)
    {
        var course = await _context.Courses
            .AsNoTracking()
            .Where(c => c.Id == request.Id)
            .Select(c => new CourseWithDetailsDto
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                ThumbnailUrl = c.ThumbnailUrl,
                IsPublished = c.IsPublished,
                HasQuiz = c.HasQuiz,
                Rating = c.Rating,
                Order = c.Order,
                Language = c.Language,
                LevelId = c.LevelId,
                LevelName = c.Level != null ? c.Level.Title : null,
                LessonsCount = c.Lessons.Count(l => l.IsPublished),
                CourseDuration = null, // calculated below after fetch
                Tags = c.CourseTags.Select(ct => new Tags.Queries.TagBriefDto
                {
                    Id = ct.Tag!.Id,
                    Title = ct.Tag.Title,
                    Description = ct.Tag.Description,
                    TotalCourses = ct.Tag.CourseTags.Count,
                    TotalQAs = ct.Tag.QATags.Count
                }).ToList(),
                Lessons = c.Lessons.OrderBy(l => l.Order).Select(l => new CourseLessonDto
                {
                    LessonId = l.Id,
                    LessonName = l.Title,
                    LessonType = l.Type,
                    LessonDuration = l.Duration,
                    IsPublished = l.IsPublished,
                    LessonHasQuiz = l.HasQuiz,
                    IsLessonQuizPassed = false,
                    IsLessonCompleted = false,
                    QuizName = null
                }).ToList(),
                Prerequisites = c.CoursePrerequisites.OrderBy(cp => cp.Order).Select(cp => new PrerequisiteCourseDto
                {
                    Id = cp.PrerequisiteCourseId,
                    Title = cp.PrerequisiteCourse!.Title,
                    Order = cp.Order
                }).ToList(),
                UnlockedCourses = c.PrerequisiteForCourses.Select(cp => new PrerequisiteCourseDto
                {
                    Id = cp.CourseId,
                    Title = cp.Course!.Title,
                    Order = cp.Order
                }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (course == null)
        {
            return Result<CourseWithDetailsDto?>.Success(null);
        }

        // Calculate total duration from fetched lessons in-memory
        course.CourseDuration = course.Lessons
            .Where(l => l.LessonDuration.HasValue)
            .Select(l => l.LessonDuration!.Value)
            .Aggregate(TimeSpan.Zero, (acc, ts) => acc + ts);

        // Attempt to resolve student from current user
        Guid? studentId = null;
        if (Guid.TryParse(_currentUser.Id, out var currentUserId))
        {
            var student = await _context.Students
                .AsNoTracking()
                .Where(s => s.UserId == currentUserId)
                .Select(s => new { s.Id })
                .FirstOrDefaultAsync(cancellationToken);

            if (student != null)
                studentId = student.Id;
        }

        if (studentId.HasValue)
        {
            var sid = studentId.Value;

            // Enrollment status
            var enrollment = await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.StudentId == sid && e.CourseId == request.Id)
                .Select(e => new { e.Status, e.TotalLessonsCount, e.CompletedLessonsCount })
                .FirstOrDefaultAsync(cancellationToken);

            if (enrollment != null)
            {
                course.IsCompleted = enrollment.Status == EnrollmentStatus.Completed;
                course.CourseProgress = enrollment.TotalLessonsCount == 0
                    ? 0
                    : Math.Round(((double)enrollment.CompletedLessonsCount / enrollment.TotalLessonsCount) * 100, 2);
            }

            // Completed lesson IDs for this course
            var completedLessonIds = await _context.StudentProgresses
                .AsNoTracking()
                .Where(sp => sp.StudentId == sid && sp.CourseId == request.Id && sp.IsCompleted)
                .Select(sp => sp.LessonId)
                .ToListAsync(cancellationToken);

            // Lesson quiz attempts passed
            var lessonQuizPassedIds = await _context.QuizAttempts
                .AsNoTracking()
                .Where(a => a.StudentId == sid && a.IsPassed && a.Quiz != null && a.Quiz.LessonId != null)
                .Select(a => a.Quiz!.LessonId!.Value)
                .ToListAsync(cancellationToken);

            foreach (var lesson in course.Lessons)
            {
                lesson.IsLessonCompleted = completedLessonIds.Contains(lesson.LessonId);
                lesson.IsLessonQuizPassed = lessonQuizPassedIds.Contains(lesson.LessonId);
            }

            // Completed prerequisite courses
            var completedCourseIds = await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.StudentId == sid && e.Status == EnrollmentStatus.Completed)
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
        else
        {
            course.CourseProgress = 0;
        }

        return Result<CourseWithDetailsDto?>.Success(course);
    }
}
