using AskAMuslimBackend.Application.Common.Interfaces;
using FluentValidation;

namespace AskAMuslimBackend.Application.Levels.Commands.DeleteLevel;

public record DeleteLevelCommand(Guid Id) : IRequest;

public class DeleteLevelCommandHandler : IRequestHandler<DeleteLevelCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteLevelCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteLevelCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Levels.FindAsync(new object[] { request.Id }, cancellationToken);
        Guard.Against.NotFound(request.Id, entity);

        _context.Levels.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class DeleteLevelCommandValidator : AbstractValidator<DeleteLevelCommand>
{
    public DeleteLevelCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Level Id is required.");
    }
}
