using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;

namespace AskAMuslimBackend.Application.Quizzes.Commands.UpdateQuiz;

public record UpdateQuizCommand : IRequest<Result>
{
    public Guid Id { get; init; }
    public Guid? LevelId { get; init; }
    public Guid? CourseId { get; init; }
    public Guid? LessonId { get; init; }
    public QuizTargetType TargetType { get; init; }
    public string Title { get; init; } = string.Empty;
    public double TotalMarks { get; init; }
}

public class UpdateQuizCommandHandler : IRequestHandler<UpdateQuizCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateQuizCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UpdateQuizCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Quizzes.FindAsync(new object[] { request.Id }, cancellationToken);
        if (entity == null)
        {
            return Result.Failure(new[] { "Quiz not found." });
        }

        // Validate target exists if changed
        var targetExists = request.TargetType switch
        {
            QuizTargetType.Level => request.LevelId.HasValue && await _context.Levels.AnyAsync(l => l.Id == request.LevelId.Value, cancellationToken),
            QuizTargetType.Course => request.CourseId.HasValue && await _context.Courses.AnyAsync(c => c.Id == request.CourseId.Value, cancellationToken),
            QuizTargetType.Lesson => request.LessonId.HasValue && await _context.Lessons.AnyAsync(l => l.Id == request.LessonId.Value, cancellationToken),
            _ => false
        };
        if (!targetExists)
        {
            return Result.Failure(new[] { "Target does not exist or id not provided." });
        }

        entity.LevelId = request.LevelId;
        entity.CourseId = request.CourseId;
        entity.LessonId = request.LessonId;
        entity.TargetType = request.TargetType;
        entity.Title = request.Title;
        entity.TotalMarks = request.TotalMarks;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public class UpdateQuizCommandValidator : AbstractValidator<UpdateQuizCommand>
{
    public UpdateQuizCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.TargetType).IsInEnum();
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");
        RuleFor(x => x.TotalMarks)
            .GreaterThanOrEqualTo(0).WithMessage("TotalMarks must be 0 or greater.");

        When(x => x.TargetType == QuizTargetType.Level, () =>
        {
            RuleFor(x => x.LevelId).NotEmpty().WithMessage("LevelId is required for a level quiz.");
        });
        When(x => x.TargetType == QuizTargetType.Course, () =>
        {
            RuleFor(x => x.CourseId).NotEmpty().WithMessage("CourseId is required for a course quiz.");
        });
        When(x => x.TargetType == QuizTargetType.Lesson, () =>
        {
            RuleFor(x => x.LessonId).NotEmpty().WithMessage("LessonId is required for a lesson quiz.");
        });
    }
}
