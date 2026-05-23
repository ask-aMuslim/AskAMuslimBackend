using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace AskAMuslimBackend.Application.Lessons.Commands.DeleteLesson;

public record DeleteLessonCommand([property: JsonIgnore] Guid Id) : IRequest<Result>;

public class DeleteLessonCommandHandler : IRequestHandler<DeleteLessonCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ILocalizationService _localizer;

    public DeleteLessonCommandHandler(IApplicationDbContext context, ILocalizationService localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    public async Task<Result> Handle(DeleteLessonCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Lessons.FindAsync(new object[] { request.Id }, cancellationToken);

        if (entity == null)
            return Result.Failure(new[] { _localizer["Lessons.NotFound"] });

        // 1. Delete all StudentProgress records for this lesson
        await _context.StudentProgresses
            .Where(sp => sp.LessonId == request.Id)
            .ExecuteDeleteAsync(cancellationToken);

        // 2. Delete all quizzes for this lesson and their child data
        var lessonQuizIds = await _context.Quizzes
            .Where(q => q.LessonId == request.Id)
            .Select(q => q.Id)
            .ToListAsync(cancellationToken);

        if (lessonQuizIds.Any())
        {
            var attemptIds = await _context.QuizAttempts
                .Where(qa => lessonQuizIds.Contains(qa.QuizId))
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
                .Where(q => lessonQuizIds.Contains(q.QuizId))
                .Select(q => q.Id)
                .ToListAsync(cancellationToken);

            if (questionIds.Any())
            {
                await _context.Options
                    .Where(o => questionIds.Contains(o.QuestionId))
                    .ExecuteDeleteAsync(cancellationToken);

                await _context.Questions
                    .Where(q => lessonQuizIds.Contains(q.QuizId))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            await _context.Quizzes
                .Where(q => lessonQuizIds.Contains(q.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        // 3. Delete lesson-level related records explicitly
        await _context.StudentNotes
            .Where(n => n.LessonId == request.Id)
            .ExecuteDeleteAsync(cancellationToken);

        await _context.StudentQuestions
            .Where(sq => sq.LessonId == request.Id)
            .ExecuteDeleteAsync(cancellationToken);

        await _context.LessonFeedbacks
            .Where(lf => lf.LessonId == request.Id)
            .ExecuteDeleteAsync(cancellationToken);

        // 4. Finally delete the lesson
        _context.Lessons.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public class DeleteLessonCommandValidator : AbstractValidator<DeleteLessonCommand>
{
    public DeleteLessonCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Lesson Id is required.");
    }
}
