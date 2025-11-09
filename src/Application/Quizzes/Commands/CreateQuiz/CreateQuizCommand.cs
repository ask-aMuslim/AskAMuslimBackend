using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;

namespace AskAMuslimBackend.Application.Quizzes.Commands.CreateQuiz;

public record CreateQuizCommand : IRequest<Guid>
{
    public Guid? LevelId { get; init; }
    public Guid? CourseId { get; init; }
    public Guid? LessonId { get; init; }
    public QuizTargetType TargetType { get; init; }
    public string Title { get; init; } = string.Empty;
    public double TotalMarks { get; init; }
}

public class CreateQuizCommandHandler : IRequestHandler<CreateQuizCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateQuizCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateQuizCommand request, CancellationToken cancellationToken)
    {
        // Determine target id based on type and validate existence
        var targetExists = request.TargetType switch
        {
            QuizTargetType.Level => request.LevelId.HasValue && await _context.Levels.AnyAsync(l => l.Id == request.LevelId.Value, cancellationToken),
            QuizTargetType.Course => request.CourseId.HasValue && await _context.Courses.AnyAsync(c => c.Id == request.CourseId.Value, cancellationToken),
            QuizTargetType.Lesson => request.LessonId.HasValue && await _context.Lessons.AnyAsync(l => l.Id == request.LessonId.Value, cancellationToken),
            _ => false
        };

        if (!targetExists)
        {
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("Target", "Target does not exist or id not provided.") });
        }

        var quiz = new Quiz
        {
            LevelId = request.LevelId,
            CourseId = request.CourseId,
            LessonId = request.LessonId,
            TargetType = request.TargetType,
            Title = request.Title,
            TotalMarks = request.TotalMarks
        };

        _context.Quizzes.Add(quiz);
        await _context.SaveChangesAsync(cancellationToken);
        return quiz.Id;
    }
}

public class CreateQuizCommandValidator : AbstractValidator<CreateQuizCommand>
{
    public CreateQuizCommandValidator()
    {
        RuleFor(x => x.TargetType).IsInEnum();
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");
        RuleFor(x => x.TotalMarks)
            .GreaterThanOrEqualTo(0).WithMessage("TotalMarks must be 0 or greater.");

        // Ensure correct id supplied per target type
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
