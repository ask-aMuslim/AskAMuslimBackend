using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using System.Text.Json.Serialization;

namespace AskAMuslimBackend.Application.QAs.Commands.DeleteQA;

public record DeleteQACommand([property: JsonIgnore] Guid Id) : IRequest<Result>;

public class DeleteQACommandHandler : IRequestHandler<DeleteQACommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ILocalizationService _localizer;

    public DeleteQACommandHandler(IApplicationDbContext context, ILocalizationService localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    public async Task<Result> Handle(DeleteQACommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.QAs.FindAsync(new object[] { request.Id }, cancellationToken);
        
        if (entity == null)
            return Result.Failure(new[] { _localizer["QAs.NotFound"] });

        _context.QAs.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
