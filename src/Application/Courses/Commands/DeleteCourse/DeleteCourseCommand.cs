using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
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

            // 3. Delete all StudentProgress records for this course
            await _context.StudentProgresses
                .Where(sp => sp.CourseId == request.Id)
                .ExecuteDeleteAsync(cancellationToken);

            // 4. Delete all enrollments for this course
            await _context.Enrollments
                .Where(e => e.CourseId == request.Id)
                .ExecuteDeleteAsync(cancellationToken);

            // 5. Delete all lesson-level related records (StudentNotes, StudentQuestions, LessonFeedbacks)
            var lessonIds = await _context.Lessons
                .Where(l => l.CourseId == request.Id)
                .Select(l => l.Id)
                .ToListAsync(cancellationToken);

            if (lessonIds.Any())
            {
                await _context.StudentNotes
                    .Where(n => lessonIds.Contains(n.LessonId))
                    .ExecuteDeleteAsync(cancellationToken);

                await _context.StudentQuestions
                    .Where(sq => lessonIds.Contains(sq.LessonId))
                    .ExecuteDeleteAsync(cancellationToken);

                await _context.LessonFeedbacks
                    .Where(lf => lessonIds.Contains(lf.LessonId))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            // 6. Delete all quizzes for this course and their child data
            var allQuizIds = await _context.Quizzes
                .Where(q => q.CourseId == request.Id)
                .Select(q => q.Id)
                .ToListAsync(cancellationToken);

            if (allQuizIds.Any())
            {
                var attemptIds = await _context.QuizAttempts
                    .Where(qa => allQuizIds.Contains(qa.QuizId))
                    .Select(qa => qa.Id)
                    .ToListAsync(cancellationToken);

                if (attemptIds.Any())
                {
                    await _context.QuizAttemptAnswers
                        .Where(a => attemptIds.Contains(a.QuizAttemptId))
                        .ExecuteDeleteAsync(cancellationToken);

                    await _context.QuizAttempts
                        .Where(qa => attemptIds.Contains(qa.Id))
                        .ExecuteDeleteAsync(cancellationToken);
                }

                var questionIds = await _context.Questions
                    .Where(q => allQuizIds.Contains(q.QuizId))
                    .Select(q => q.Id)
                    .ToListAsync(cancellationToken);

                if (questionIds.Any())
                {
                    await _context.Options
                        .Where(o => questionIds.Contains(o.QuestionId))
                        .ExecuteDeleteAsync(cancellationToken);

                    await _context.Questions
                        .Where(q => allQuizIds.Contains(q.QuizId))
                        .ExecuteDeleteAsync(cancellationToken);
                }

                await _context.Quizzes
                    .Where(q => allQuizIds.Contains(q.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            // 7. Delete all lessons
            await _context.Lessons
                .Where(l => l.CourseId == request.Id)
                .ExecuteDeleteAsync(cancellationToken);

            // 8. Delete course tags
            await _context.CourseTags
                .Where(ct => ct.CourseId == request.Id)
                .ExecuteDeleteAsync(cancellationToken);

            // 9. Delete course prerequisites
            await _context.CoursePrerequisites
                .Where(cp => cp.CourseId == request.Id)
                .ExecuteDeleteAsync(cancellationToken);

            // 10. Finally, delete the course
            await _context.Courses
                .Where(c => c.Id == request.Id)
                .ExecuteDeleteAsync(cancellationToken);

            return Result.Success();
        }
        catch (DbUpdateException ex)
        {
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
