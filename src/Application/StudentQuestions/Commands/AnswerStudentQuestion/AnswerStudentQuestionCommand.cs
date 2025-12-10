using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using FluentValidation;

namespace AskAMuslimBackend.Application.StudentQuestions.Commands.AnswerStudentQuestion;

public record AnswerStudentQuestionCommand : IRequest<Result>
{
    public Guid Id { get; init; }
    public string AnswerText { get; init; } = string.Empty;
}

public class AnswerStudentQuestionCommandHandler : IRequestHandler<AnswerStudentQuestionCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public AnswerStudentQuestionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(AnswerStudentQuestionCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.StudentQuestions.FindAsync(new object[] { request.Id }, cancellationToken);
        
        if (entity == null)
            return Result.Failure(new[] { "Student question not found." });

        entity.Answered = true;
        entity.AnswerText = request.AnswerText;
        entity.AnsweredAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public class AnswerStudentQuestionCommandValidator : AbstractValidator<AnswerStudentQuestionCommand>
{
    public AnswerStudentQuestionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.AnswerText)
            .NotEmpty().WithMessage("AnswerText is required.")
            .MaximumLength(5000).WithMessage("AnswerText must not exceed 5000 characters.");
    }
}
