using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace AskAMuslimBackend.Application.Levels.Commands.DeleteLevel;

public record DeleteLevelCommand([property: JsonIgnore] Guid Id) : IRequest<Result>;

public class DeleteLevelCommandHandler : IRequestHandler<DeleteLevelCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ILocalizationService _localizer;

    public DeleteLevelCommandHandler(IApplicationDbContext context, ILocalizationService localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    public async Task<Result> Handle(DeleteLevelCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Levels.FindAsync(new object[] { request.Id }, cancellationToken);
        if (entity == null)
        {
            return Result.Failure(new[] { _localizer["Levels.NotFound"] });
        }

        try
        {
            // 1. Gather all course IDs in this level
            var courseIdsInLevel = await _context.Courses
                .Where(c => c.LevelId == request.Id)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            // 2. Gather all lesson IDs for all courses in this level
            var lessonIds = await _context.Lessons
                .Where(l => courseIdsInLevel.Contains(l.CourseId))
                .Select(l => l.Id)
                .ToListAsync(cancellationToken);

            // 3. Drop prerequisite references where a course in this level is the prerequisite
            if (courseIdsInLevel.Any())
            {
                await _context.CoursePrerequisites
                    .Where(cp => courseIdsInLevel.Contains(cp.PrerequisiteCourseId))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            // 4. Collect all quiz IDs (level + course + lesson scope)
            var levelQuizIds = await _context.Quizzes
                .Where(q => q.LevelId == request.Id)
                .Select(q => q.Id)
                .ToListAsync(cancellationToken);

            var courseQuizIds = await _context.Quizzes
                .Where(q => q.CourseId.HasValue && courseIdsInLevel.Contains(q.CourseId.Value))
                .Select(q => q.Id)
                .ToListAsync(cancellationToken);

            var lessonQuizIds = await _context.Quizzes
                .Where(q => q.LessonId.HasValue && lessonIds.Contains(q.LessonId.Value))
                .Select(q => q.Id)
                .ToListAsync(cancellationToken);

            var allQuizIds = levelQuizIds
                .Concat(courseQuizIds)
                .Concat(lessonQuizIds)
                .Distinct()
                .ToList();

            // 5. Delete all quiz child data and quizzes
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

            // 6. Delete all course-scoped child data
            if (courseIdsInLevel.Any())
            {
                await _context.StudentProgresses
                    .Where(sp => courseIdsInLevel.Contains(sp.CourseId) || lessonIds.Contains(sp.LessonId))
                    .ExecuteDeleteAsync(cancellationToken);

                await _context.Enrollments
                    .Where(e => courseIdsInLevel.Contains(e.CourseId))
                    .ExecuteDeleteAsync(cancellationToken);
            }

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

            if (courseIdsInLevel.Any())
            {
                await _context.Lessons
                    .Where(l => courseIdsInLevel.Contains(l.CourseId))
                    .ExecuteDeleteAsync(cancellationToken);

                await _context.CourseTags
                    .Where(ct => courseIdsInLevel.Contains(ct.CourseId))
                    .ExecuteDeleteAsync(cancellationToken);

                await _context.CoursePrerequisites
                    .Where(cp => courseIdsInLevel.Contains(cp.CourseId))
                    .ExecuteDeleteAsync(cancellationToken);

                await _context.Courses
                    .Where(c => c.LevelId == request.Id)
                    .ExecuteDeleteAsync(cancellationToken);
            }

            // 7. Delete certificates linked to this level
            await _context.Certificates
                .Where(c => c.LevelId == request.Id)
                .ExecuteDeleteAsync(cancellationToken);

            // 8. Finally delete the level
            _context.Levels.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (DbUpdateException ex)
        {
            return Result.Failure(new[]
            {
                "Failed to delete level due to database constraints.",
                $"Technical details: {ex.InnerException?.Message ?? ex.Message}"
            });
        }
        catch (Exception ex)
        {
            return Result.Failure(new[]
            {
                "An unexpected error occurred while deleting the level.",
                $"Error: {ex.Message}"
            });
        }
    }
}

public class DeleteLevelCommandValidator : AbstractValidator<DeleteLevelCommand>
{
    public DeleteLevelCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Level Id is required.");
    }
}
