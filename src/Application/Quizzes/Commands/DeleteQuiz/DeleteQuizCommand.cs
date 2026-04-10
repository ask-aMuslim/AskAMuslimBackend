using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using FluentValidation;
using System.Text.Json.Serialization;

namespace AskAMuslimBackend.Application.Quizzes.Commands.DeleteQuiz;

public record DeleteQuizCommand([property: JsonIgnore] Guid Id) : IRequest<Result>;

public class DeleteQuizCommandHandler : IRequestHandler<DeleteQuizCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ILocalizationService _localizer;

    public DeleteQuizCommandHandler(IApplicationDbContext context, ILocalizationService localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    public async Task<Result> Handle(DeleteQuizCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Quizzes.FindAsync(new object[] { request.Id }, cancellationToken);
        if (entity == null)
        {
            return Result.Failure(new[] { _localizer["Quizzes.NotFound"] });
        }

        _context.Quizzes.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        // Re-evaluate the HasQuiz flag for Course or Lesson
        if (entity.TargetType == Domain.Enums.QuizTargetType.Course && entity.CourseId.HasValue)
        {
            bool hasMoreQuizzes = await _context.Quizzes.AnyAsync(q => q.TargetType == Domain.Enums.QuizTargetType.Course && q.CourseId == entity.CourseId.Value, cancellationToken);
            if (!hasMoreQuizzes)
            {
                var course = await _context.Courses.FindAsync(new object[] { entity.CourseId.Value }, cancellationToken);
                if (course != null)
                {
                    course.HasQuiz = false;
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }
        }
        else if (entity.TargetType == Domain.Enums.QuizTargetType.Lesson && entity.LessonId.HasValue)
        {
            bool hasMoreQuizzes = await _context.Quizzes.AnyAsync(q => q.TargetType == Domain.Enums.QuizTargetType.Lesson && q.LessonId == entity.LessonId.Value, cancellationToken);
            if (!hasMoreQuizzes)
            {
                var lesson = await _context.Lessons.FindAsync(new object[] { entity.LessonId.Value }, cancellationToken);
                if (lesson != null)
                {
                    lesson.HasQuiz = false;
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }
        }
        else if (entity.TargetType == Domain.Enums.QuizTargetType.Level && entity.LevelId.HasValue)
        {
            bool hasMoreQuizzes = await _context.Quizzes.AnyAsync(q => q.TargetType == Domain.Enums.QuizTargetType.Level && q.LevelId == entity.LevelId.Value, cancellationToken);
            if (!hasMoreQuizzes)
            {
                var level = await _context.Levels.FindAsync(new object[] { entity.LevelId.Value }, cancellationToken);
                if (level != null)
                {
                    level.HasQuiz = false;
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }
        }

        return Result.Success();
    }
}

public class DeleteQuizCommandValidator : AbstractValidator<DeleteQuizCommand>
{
    public DeleteQuizCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
