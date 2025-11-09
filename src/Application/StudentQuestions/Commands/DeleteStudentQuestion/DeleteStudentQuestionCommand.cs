using AskAMuslimBackend.Application.Common.Interfaces;
using FluentValidation;

namespace AskAMuslimBackend.Application.StudentQuestions.Commands.DeleteStudentQuestion;

public record DeleteStudentQuestionCommand(Guid Id) : IRequest;

public class DeleteStudentQuestionCommandHandler : IRequestHandler<DeleteStudentQuestionCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteStudentQuestionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteStudentQuestionCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.StudentQuestions.FindAsync(new object[] { request.Id }, cancellationToken);
        Guard.Against.NotFound(request.Id, entity);

        _context.StudentQuestions.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class DeleteStudentQuestionCommandValidator : AbstractValidator<DeleteStudentQuestionCommand>
{
    public DeleteStudentQuestionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
