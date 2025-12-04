using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using FluentValidation;

namespace AskAMuslimBackend.Application.Levels.Commands.DeleteLevel;

public record DeleteLevelCommand(Guid Id) : IRequest<Result>;

public class DeleteLevelCommandHandler : IRequestHandler<DeleteLevelCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteLevelCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteLevelCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Levels.FindAsync(new object[] { request.Id }, cancellationToken);
        
        if (entity == null)
        {
            return Result.Failure(new[] { "Level not found" });
        }

        _context.Levels.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
    }
}

public class DeleteLevelCommandValidator : AbstractValidator<DeleteLevelCommand>
{
    public DeleteLevelCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Level Id is required.");
    }
}
