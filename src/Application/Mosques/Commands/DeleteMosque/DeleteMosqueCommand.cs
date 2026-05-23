using System.Text.Json.Serialization;
using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Security;
using AskAMuslimBackend.Domain.Constants;

namespace AskAMuslimBackend.Application.Mosques.Commands.DeleteMosque;

[Authorize(Policy = Policies.ManageAdmins)]
public record DeleteMosqueCommand([property: JsonIgnore] Guid Id) : IRequest<Result>;

public class DeleteMosqueCommandHandler : IRequestHandler<DeleteMosqueCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteMosqueCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteMosqueCommand request, CancellationToken cancellationToken)
    {
        var mosque = await _context.Mosques.FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);
        if (mosque == null)
        {
            return Result.Failure(new[] { "Mosque not found" });
        }

        _context.Mosques.Remove(mosque);
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public class DeleteMosqueCommandValidator : AbstractValidator<DeleteMosqueCommand>
{
    public DeleteMosqueCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
