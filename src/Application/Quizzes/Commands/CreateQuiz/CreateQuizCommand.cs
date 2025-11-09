using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;

namespace AskAMuslimBackend.Application.Quizzes.Commands.CreateQuiz;

public record CreateQuizCommand : IRequest<Guid>
{
    public Guid TargetId { get; init; }
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
        // Validate target existence manually (FK is not on Quiz for polymorphic relation) based on TargetType
        var targetExists = request.TargetType switch
        {
            QuizTargetType.Level => await _context.Levels.AnyAsync(l => l.Id == request.TargetId, cancellationToken),
            QuizTargetType.Course => await _context.Courses.AnyAsync(c => c.Id == request.TargetId, cancellationToken),
            QuizTargetType.Lesson => await _context.Lessons.AnyAsync(l => l.Id == request.TargetId, cancellationToken),
            _ => false
        };

        if (!targetExists)
        {
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("TargetId", "Target does not exist.") });
        }

        var quiz = new Quiz
        {
            TargetId = request.TargetId,
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
        RuleFor(x => x.TargetId).NotEmpty();
        RuleFor(x => x.TargetType).IsInEnum();
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");
        RuleFor(x => x.TotalMarks)
            .GreaterThanOrEqualTo(0).WithMessage("TotalMarks must be 0 or greater.");
    }
}
