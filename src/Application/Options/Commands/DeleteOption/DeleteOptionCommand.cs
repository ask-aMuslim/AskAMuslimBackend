using AskAMuslimBackend.Application.Common.Interfaces;
using FluentValidation;

namespace AskAMuslimBackend.Application.Options.Commands.DeleteOption;

public record DeleteOptionCommand(Guid Id) : IRequest;

public class DeleteOptionCommandHandler : IRequestHandler<DeleteOptionCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteOptionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteOptionCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Options.FindAsync(new object[] { request.Id }, cancellationToken);
        Guard.Against.NotFound(request.Id, entity);

        _context.Options.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class DeleteOptionCommandValidator : AbstractValidator<DeleteOptionCommand>
{
    public DeleteOptionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
