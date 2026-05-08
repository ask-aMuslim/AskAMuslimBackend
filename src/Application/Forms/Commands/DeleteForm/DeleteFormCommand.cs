using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using FluentValidation;
using System.Text.Json.Serialization;

namespace AskAMuslimBackend.Application.Forms.Commands.DeleteForm;

public record DeleteFormCommand([property: JsonIgnore] Guid Id) : IRequest<Result>;

public class DeleteFormCommandHandler : IRequestHandler<DeleteFormCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteFormCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteFormCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Forms.FindAsync(new object[] { request.Id }, cancellationToken);
        if (entity == null)
        {
            return Result.Failure(new[] { "Form not found" });
        }

        _context.Forms.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public class DeleteFormCommandValidator : AbstractValidator<DeleteFormCommand>
{
    public DeleteFormCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
