using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace AskAMuslimBackend.Application.EventRegistrations.Commands.DeleteEventRegistration;

public record DeleteEventRegistrationCommand([property: JsonIgnore] Guid Id) : IRequest<Result>;

public class DeleteEventRegistrationCommandHandler : IRequestHandler<DeleteEventRegistrationCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ILocalizationService _localizer;

    public DeleteEventRegistrationCommandHandler(IApplicationDbContext context, ILocalizationService localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    public async Task<Result> Handle(DeleteEventRegistrationCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.EventRegistrations.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
        if (entity == null)
        {
            return Result.Failure(new[] { _localizer["EventRegistrations.NotFound"] });
        }
        
        _context.EventRegistrations.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
