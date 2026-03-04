using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace AskAMuslimBackend.Application.Courses.Commands.DeleteCourse;

public record DeleteCourseCommand([property: JsonIgnore] Guid Id) : IRequest<Result>;

public class DeleteCourseCommandHandler : IRequestHandler<DeleteCourseCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ILocalizationService _localizer;

    public DeleteCourseCommandHandler(IApplicationDbContext context, ILocalizationService localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    public async Task<Result> Handle(DeleteCourseCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Check if course exists
            var courseExists = await _context.Courses
                .AnyAsync(c => c.Id == request.Id, cancellationToken);

            if (!courseExists)
            {
                return Result.Failure(new[] { _localizer["Courses.NotFound"] });
            }

            // 2. Check if course is a prerequisite for other courses (single query)
            var dependentCourses = await _context.CoursePrerequisites
                .Where(cp => cp.PrerequisiteCourseId == request.Id)
                .Select(cp => cp.Course!.Title)
                .Take(5) // Limit for error message
                .ToListAsync(cancellationToken);

            if (dependentCourses.Any())
            {
                var courseList = string.Join(", ", dependentCourses.Take(3));
                var moreCount = dependentCourses.Count > 3 ? $" and {dependentCourses.Count - 3} more" : "";
                return Result.Failure(new[] 
                { 
                    _localizer.GetString("Courses.IsPrerequisiteFor", $"{courseList}{moreCount}")
                });
            }

            // 3. Check if course has lessons
            var lessonCount = await _context.Lessons
                .CountAsync(l => l.CourseId == request.Id, cancellationToken);

            if (lessonCount > 0)
            {
                return Result.Failure(new[] 
                { 
                    _localizer.GetString("Courses.HasLessons", lessonCount)
                });
            }

            // 4. Check for active enrollments (single query)
            var activeEnrollmentInfo = await _context.Enrollments
                .Where(e => e.CourseId == request.Id && e.Status == Domain.Enums.EnrollmentStatus.Active)
                .Select(e => new { e.StudentId })
                .Take(1)
                .AnyAsync(cancellationToken);

            if (activeEnrollmentInfo)
            {
                var activeCount = await _context.Enrollments
                    .CountAsync(e => e.CourseId == request.Id && e.Status == Domain.Enums.EnrollmentStatus.Active, 
                        cancellationToken);

                return Result.Failure(new[] 
                { 
                    _localizer.GetString("Courses.HasActiveEnrollments", activeCount)
                });
            }

            // 5. Get all quiz IDs (course quizzes only, since lessons are blocked from deletion)
            var allQuizIds = await _context.Quizzes
                .Where(q => q.CourseId == request.Id)
                .Select(q => q.Id)
                .ToListAsync(cancellationToken);

            if (allQuizIds.Any())
            {
                // 6. Get quiz attempt IDs (single query)
                var attemptIds = await _context.QuizAttempts
                    .Where(qa => allQuizIds.Contains(qa.QuizId))
                    .Select(qa => qa.Id)
                    .ToListAsync(cancellationToken);

                // 7. Bulk delete quiz attempt answers
                if (attemptIds.Any())
                {
                    await _context.QuizAttemptAnswers
                        .Where(a => attemptIds.Contains(a.QuizAttemptId))
                        .ExecuteDeleteAsync(cancellationToken);
                }

                // 8. Bulk delete quiz attempts
                await _context.QuizAttempts
                    .Where(qa => allQuizIds.Contains(qa.QuizId))
                    .ExecuteDeleteAsync(cancellationToken);

                // 9. Get question IDs (single query)
                var questionIds = await _context.Questions
                    .Where(q => allQuizIds.Contains(q.QuizId))
                    .Select(q => q.Id)
                    .ToListAsync(cancellationToken);

                // 10. Bulk delete options
                if (questionIds.Any())
                {
                    await _context.Options
                        .Where(o => questionIds.Contains(o.QuestionId))
                        .ExecuteDeleteAsync(cancellationToken);
                }

                // 11. Bulk delete questions
                await _context.Questions
                    .Where(q => allQuizIds.Contains(q.QuizId))
                    .ExecuteDeleteAsync(cancellationToken);

                // 12. Bulk delete quizzes
                await _context.Quizzes
                    .Where(q => allQuizIds.Contains(q.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            // 13. Bulk delete course tags
            await _context.CourseTags
                .Where(ct => ct.CourseId == request.Id)
                .ExecuteDeleteAsync(cancellationToken);

            // 14. Bulk delete course prerequisites
            await _context.CoursePrerequisites
                .Where(cp => cp.CourseId == request.Id)
                .ExecuteDeleteAsync(cancellationToken);

            // 15. Bulk delete enrollments (completed/cancelled ones only, active ones already checked)
            await _context.Enrollments
                .Where(e => e.CourseId == request.Id)
                .ExecuteDeleteAsync(cancellationToken);

            // 16. Finally, delete the course
            await _context.Courses
                .Where(c => c.Id == request.Id)
                .ExecuteDeleteAsync(cancellationToken);

            return Result.Success();
        }
        catch (DbUpdateException ex)
        {
            // Handle database-specific errors
            return Result.Failure(new[] 
            { 
                _localizer["Courses.DeleteDbError"],
                $"Technical details: {ex.InnerException?.Message ?? ex.Message}"
            });
        }
        catch (Exception ex)
        {
            return Result.Failure(new[] 
            { 
                _localizer["Courses.DeleteUnexpectedError"],
                $"Error: {ex.Message}"
            });
        }
    }
}
