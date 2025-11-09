using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;

namespace AskAMuslimBackend.Application.Quizzes.Commands.UpdateQuiz;

public record UpdateQuizCommand : IRequest
{
    public Guid Id { get; init; }
    public Guid TargetId { get; init; }
    public QuizTargetType TargetType { get; init; }
    public string Title { get; init; } = string.Empty;
    public double TotalMarks { get; init; }
}

public class UpdateQuizCommandHandler : IRequestHandler<UpdateQuizCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateQuizCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateQuizCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Quizzes.FindAsync(new object[] { request.Id }, cancellationToken);
        Guard.Against.NotFound(request.Id, entity);

        // Validate target exists if changed
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

        entity.TargetId = request.TargetId;
        entity.TargetType = request.TargetType;
        entity.Title = request.Title;
        entity.TotalMarks = request.TotalMarks;

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class UpdateQuizCommandValidator : AbstractValidator<UpdateQuizCommand>
{
    public UpdateQuizCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.TargetId).NotEmpty();
        RuleFor(x => x.TargetType).IsInEnum();
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");
        RuleFor(x => x.TotalMarks)
            .GreaterThanOrEqualTo(0).WithMessage("TotalMarks must be 0 or greater.");
    }
}
