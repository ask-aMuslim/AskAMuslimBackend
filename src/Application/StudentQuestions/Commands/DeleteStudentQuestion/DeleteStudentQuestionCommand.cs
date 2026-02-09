using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using FluentValidation;
using System.Text.Json.Serialization;

namespace AskAMuslimBackend.Application.StudentQuestions.Commands.DeleteStudentQuestion;

public record DeleteStudentQuestionCommand([property: JsonIgnore] Guid Id) : IRequest<Result>;

public class DeleteStudentQuestionCommandHandler : IRequestHandler<DeleteStudentQuestionCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteStudentQuestionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteStudentQuestionCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.StudentQuestions.FindAsync(new object[] { request.Id }, cancellationToken);
        
        if (entity == null)
            return Result.Failure(new[] { "Student question not found." });

        _context.StudentQuestions.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public class DeleteStudentQuestionCommandValidator : AbstractValidator<DeleteStudentQuestionCommand>
{
    public DeleteStudentQuestionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
