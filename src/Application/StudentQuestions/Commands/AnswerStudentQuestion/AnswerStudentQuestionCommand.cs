using AskAMuslimBackend.Application.Common.Interfaces;
using FluentValidation;

namespace AskAMuslimBackend.Application.StudentQuestions.Commands.AnswerStudentQuestion;

public record AnswerStudentQuestionCommand : IRequest
{
    public Guid Id { get; init; }
    public string AnswerText { get; init; } = string.Empty;
}

public class AnswerStudentQuestionCommandHandler : IRequestHandler<AnswerStudentQuestionCommand>
{
    private readonly IApplicationDbContext _context;

    public AnswerStudentQuestionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(AnswerStudentQuestionCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.StudentQuestions.FindAsync(new object[] { request.Id }, cancellationToken);
        Guard.Against.NotFound(request.Id, entity);

        entity.Answered = true;
        entity.AnswerText = request.AnswerText;

        await _context.SaveChangesAsync(cancellationToken);
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
