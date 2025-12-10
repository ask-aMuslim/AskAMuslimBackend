using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.QuizAttempts.Commands.CompleteQuizAttempt;

public record CompleteQuizAttemptCommand : IRequest<Result>
{
    public Guid AttemptId { get; init; }
    public decimal Score { get; init; }
    public bool IsPassed { get; init; }
}

public class CompleteQuizAttemptCommandHandler : IRequestHandler<CompleteQuizAttemptCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public CompleteQuizAttemptCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(CompleteQuizAttemptCommand request, CancellationToken cancellationToken)
    {
        var attempt = await _context.QuizAttempts
            .Include(a => a.Quiz)
            .FirstOrDefaultAsync(a => a.Id == request.AttemptId, cancellationToken);
        if (attempt == null)
        {
            return Result.Failure(new[] { "Quiz attempt not found." });
        }

        attempt.Score = request.Score;
        attempt.IsPassed = request.IsPassed;
        attempt.CompletedAt = DateTime.UtcNow;

        // Save the attempt first so subsequent queries reflect the updated IsPassed flag
        await _context.SaveChangesAsync(cancellationToken);

        Guid? courseId = null;
        Guid? levelId = attempt.Quiz?.LevelId;

        // Resolve courseId from quiz target type: Course quiz or Lesson quiz
        if (attempt.Quiz?.CourseId is Guid cid)
        {
            courseId = cid;
        }
        else if (attempt.Quiz?.LessonId is Guid lid)
        {
            courseId = await _context.Lessons
                .Where(l => l.Id == lid)
                .Select(l => (Guid?)l.CourseId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        // If we have a course, update the enrollment's course progress
        if (courseId.HasValue)
        {
            // Find enrollment for this student & course
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.StudentId == attempt.StudentId && e.CourseId == courseId.Value, cancellationToken);

            if (enrollment != null)
            {
                // Gather all quizzes directly assigned to this course
                var directCourseQuizIdsQuery = _context.Quizzes
                    .Where(q => q.CourseId == courseId.Value)
                    .Select(q => q.Id);

                // And all quizzes assigned to the lessons of this course
                var courseLessonIdsQuery = _context.Lessons
                    .Where(l => l.CourseId == courseId.Value)
                    .Select(l => l.Id);

                var lessonQuizIdsQuery = _context.Quizzes
                    .Where(q => q.LessonId != null && courseLessonIdsQuery.Contains(q.LessonId!.Value))
                    .Select(q => q.Id);

                var courseQuizIds = await directCourseQuizIdsQuery
                    .Union(lessonQuizIdsQuery)
                    .ToListAsync(cancellationToken);

                var totalCourseQuizzes = courseQuizIds.Count;
                double courseProgress = 0d;
                if (totalCourseQuizzes > 0)
                {
                    var passedQuizCount = await _context.QuizAttempts
                        .Where(a => a.StudentId == attempt.StudentId && a.IsPassed && courseQuizIds.Contains(a.QuizId))
                        .Select(a => a.QuizId)
                        .Distinct()
                        .CountAsync(cancellationToken);

                    courseProgress = passedQuizCount * 100d / totalCourseQuizzes;
                }

                enrollment.CourseProgress = Math.Round(courseProgress, 2);

                // Mark course completed when reaching 100% and persist before computing level progress
                if (enrollment.CourseProgress >= 100d && enrollment.Status != Domain.Enums.EnrollmentStatus.Completed)
                {
                    enrollment.Status = Domain.Enums.EnrollmentStatus.Completed;
                    await _context.SaveChangesAsync(cancellationToken);
                }

                // Resolve levelId (from quiz or from the course)
                levelId ??= await _context.Courses
                    .Where(c => c.Id == courseId.Value)
                    .Select(c => (Guid?)c.LevelId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (levelId.HasValue)
                {
                    // Level progress: percentage of courses in level that are completed by student
                    var levelCourseIds = await _context.Courses
                        .Where(c => c.LevelId == levelId.Value)
                        .Select(c => c.Id)
                        .ToListAsync(cancellationToken);

                    var completedCoursesCount = await _context.Enrollments
                        .Where(e => e.StudentId == attempt.StudentId && levelCourseIds.Contains(e.CourseId) && e.Status == Domain.Enums.EnrollmentStatus.Completed)
                        .Select(e => e.CourseId)
                        .Distinct()
                        .CountAsync(cancellationToken);

                    var totalLevelCourses = levelCourseIds.Count;
                    double levelProgress = 0d;
                    if (totalLevelCourses > 0)
                    {
                        levelProgress = completedCoursesCount * 100d / totalLevelCourses;
                    }

                    var studentLevelEnrollments = await _context.Enrollments
                        .Where(e => e.StudentId == attempt.StudentId && levelCourseIds.Contains(e.CourseId))
                        .ToListAsync(cancellationToken);

                    foreach (var e in studentLevelEnrollments)
                    {
                        e.LevelProgress = Math.Round(levelProgress, 2);
                    }
                }
            }
        }
        else if (levelId.HasValue)
        {
            // Level-only quiz: recompute LevelProgress for all enrollments of this student within the level
            var levelCourseIds = await _context.Courses
                .Where(c => c.LevelId == levelId.Value)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            var completedCoursesCount = await _context.Enrollments
                .Where(e => e.StudentId == attempt.StudentId && levelCourseIds.Contains(e.CourseId) && e.Status == Domain.Enums.EnrollmentStatus.Completed)
                .Select(e => e.CourseId)
                .Distinct()
                .CountAsync(cancellationToken);

            var totalLevelCourses = levelCourseIds.Count;
            double levelProgress = 0d;
            if (totalLevelCourses > 0)
            {
                levelProgress = completedCoursesCount * 100d / totalLevelCourses;
            }

            var studentLevelEnrollments = await _context.Enrollments
                .Where(e => e.StudentId == attempt.StudentId && levelCourseIds.Contains(e.CourseId))
                .ToListAsync(cancellationToken);

            foreach (var e in studentLevelEnrollments)
            {
                e.LevelProgress = Math.Round(levelProgress, 2);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public class CompleteQuizAttemptCommandValidator : AbstractValidator<CompleteQuizAttemptCommand>
{
    public CompleteQuizAttemptCommandValidator()
    {
        RuleFor(x => x.AttemptId).NotEmpty();
        RuleFor(x => x.Score).GreaterThanOrEqualTo(0);
    }
}
