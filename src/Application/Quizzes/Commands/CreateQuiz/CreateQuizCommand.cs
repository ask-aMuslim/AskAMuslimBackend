using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;

namespace AskAMuslimBackend.Application.Quizzes.Commands.CreateQuiz;

public record CreateQuizCommand : IRequest<Result<Guid>>
{
    public Guid? LevelId { get; init; }
    public Guid? CourseId { get; init; }
    public Guid? LessonId { get; init; }
    public QuizTargetType TargetType { get; init; }
    public string Title { get; init; } = string.Empty;
    public double TotalMarks { get; init; }
}

public class CreateQuizCommandHandler : IRequestHandler<CreateQuizCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILocalizationService _localizer;

    public CreateQuizCommandHandler(IApplicationDbContext context, ILocalizationService localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    public async Task<Result<Guid>> Handle(CreateQuizCommand request, CancellationToken cancellationToken)
    {
        // Determine target id based on type and validate existence, also update HasQuiz
        bool targetExists = false;
        
        switch (request.TargetType)
        {
            case QuizTargetType.Level:
                if (request.LevelId.HasValue)
                {
                    var level = await _context.Levels.FirstOrDefaultAsync(l => l.Id == request.LevelId.Value, cancellationToken);
                    if (level != null)
                    {
                        level.HasQuiz = true;
                        targetExists = true;
                    }
                }
                break;
                
            case QuizTargetType.Course:
                if (request.CourseId.HasValue)
                {
                    var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == request.CourseId.Value, cancellationToken);
                    if (course != null)
                    {
                        course.HasQuiz = true;
                        targetExists = true;
                    }
                }
                break;
                
            case QuizTargetType.Lesson:
                if (request.LessonId.HasValue)
                {
                    var lesson = await _context.Lessons.FirstOrDefaultAsync(l => l.Id == request.LessonId.Value, cancellationToken);
                    if (lesson != null)
                    {
                        lesson.HasQuiz = true;
                        targetExists = true;
                    }
                }
                break;
        }

        if (!targetExists)
        {
            return Result<Guid>.Failure(new[] { _localizer["Quizzes.TargetDoesNotExist"] });
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
        return Result<Guid>.Success(quiz.Id);
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
