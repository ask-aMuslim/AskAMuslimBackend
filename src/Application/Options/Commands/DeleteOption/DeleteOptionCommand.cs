using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using FluentValidation;

namespace AskAMuslimBackend.Application.Options.Commands.DeleteOption;

public record DeleteOptionCommand(Guid Id) : IRequest<Result>;

public class DeleteOptionCommandHandler : IRequestHandler<DeleteOptionCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteOptionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteOptionCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Options.FindAsync(new object[] { request.Id }, cancellationToken);
        if (entity == null) return Result.Failure(new[] { "Option not found" });

        _context.Options.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public class DeleteOptionCommandValidator : AbstractValidator<DeleteOptionCommand>
{
    public DeleteOptionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
