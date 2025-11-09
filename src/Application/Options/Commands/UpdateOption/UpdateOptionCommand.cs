using AskAMuslimBackend.Application.Common.Interfaces;
using FluentValidation;

namespace AskAMuslimBackend.Application.Options.Commands.UpdateOption;

public record UpdateOptionCommand : IRequest
{
    public Guid Id { get; init; }
    public string Text { get; init; } = string.Empty;
    public bool IsCorrect { get; init; }
}

public class UpdateOptionCommandHandler : IRequestHandler<UpdateOptionCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateOptionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateOptionCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Options.FindAsync(new object[] { request.Id }, cancellationToken);
        Guard.Against.NotFound(request.Id, entity);

        entity.Text = request.Text;
        entity.IsCorrect = request.IsCorrect;

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class UpdateOptionCommandValidator : AbstractValidator<UpdateOptionCommand>
{
    public UpdateOptionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Text is required.")
            .MaximumLength(500).WithMessage("Text must not exceed 500 characters.");
    }
}
