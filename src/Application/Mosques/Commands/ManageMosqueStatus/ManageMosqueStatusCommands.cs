using System.Text.Json.Serialization;
using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Security;
using AskAMuslimBackend.Domain.Constants;

namespace AskAMuslimBackend.Application.Mosques.Commands.ManageMosqueStatus;

[Authorize(Policy = Policies.ManageAdmins)]
public record ActivateMosqueCommand([property: JsonIgnore] Guid Id) : IRequest<Result>;

[Authorize(Policy = Policies.ManageAdmins)]
public record DeactivateMosqueCommand([property: JsonIgnore] Guid Id) : IRequest<Result>;

[Authorize(Policy = Policies.ManageAdmins)]
public record VerifyMosqueCommand([property: JsonIgnore] Guid Id) : IRequest<Result>;

public class ActivateMosqueCommandHandler : IRequestHandler<ActivateMosqueCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public ActivateMosqueCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(ActivateMosqueCommand request, CancellationToken cancellationToken)
    {
        return await UpdateStatus(request.Id, mosque => mosque.IsActive = true, cancellationToken);
    }

    private async Task<Result> UpdateStatus(Guid id, Action<Domain.Entities.Mosque> updateAction, CancellationToken cancellationToken)
    {
        var mosque = await _context.Mosques.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (mosque == null)
        {
            return Result.Failure(new[] { "Mosque not found" });
        }

        updateAction(mosque);
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public class DeactivateMosqueCommandHandler : IRequestHandler<DeactivateMosqueCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeactivateMosqueCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeactivateMosqueCommand request, CancellationToken cancellationToken)
    {
        var mosque = await _context.Mosques.FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);
        if (mosque == null)
        {
            return Result.Failure(new[] { "Mosque not found" });
        }

        mosque.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public class VerifyMosqueCommandHandler : IRequestHandler<VerifyMosqueCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public VerifyMosqueCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(VerifyMosqueCommand request, CancellationToken cancellationToken)
    {
        var mosque = await _context.Mosques.FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);
        if (mosque == null)
        {
            return Result.Failure(new[] { "Mosque not found" });
        }

        mosque.IsVerified = true;
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public class ActivateMosqueCommandValidator : AbstractValidator<ActivateMosqueCommand>
{
    public ActivateMosqueCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeactivateMosqueCommandValidator : AbstractValidator<DeactivateMosqueCommand>
{
    public DeactivateMosqueCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class VerifyMosqueCommandValidator : AbstractValidator<VerifyMosqueCommand>
{
    public VerifyMosqueCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
