using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using FluentValidation;
using System.Text.Json.Serialization;

namespace AskAMuslimBackend.Application.Options.Commands.DeleteOption;

public record DeleteOptionCommand([property: JsonIgnore] Guid Id) : IRequest<Result>;

public class DeleteOptionCommandHandler : IRequestHandler<DeleteOptionCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ILocalizationService _localizer;

    public DeleteOptionCommandHandler(IApplicationDbContext context, ILocalizationService localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    public async Task<Result> Handle(DeleteOptionCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Options.FindAsync(new object[] { request.Id }, cancellationToken);
        if (entity == null) return Result.Failure(new[] { _localizer["Options.NotFound"] });

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
