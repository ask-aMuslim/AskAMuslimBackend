using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using FluentValidation;

namespace AskAMuslimBackend.Application.Quizzes.Commands.DeleteQuiz;

public record DeleteQuizCommand(Guid Id) : IRequest<Result>;

public class DeleteQuizCommandHandler : IRequestHandler<DeleteQuizCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteQuizCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteQuizCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Quizzes.FindAsync(new object[] { request.Id }, cancellationToken);
        if (entity == null)
        {
            return Result.Failure(new[] { "Quiz not found." });
        }

        _context.Quizzes.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
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
