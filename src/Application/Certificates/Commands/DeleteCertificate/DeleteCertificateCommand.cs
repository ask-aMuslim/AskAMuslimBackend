using AskAMuslimBackend.Application.Common.Interfaces;
using FluentValidation;

namespace AskAMuslimBackend.Application.Certificates.Commands.DeleteCertificate;

public record DeleteCertificateCommand(Guid Id) : IRequest;

public class DeleteCertificateCommandHandler : IRequestHandler<DeleteCertificateCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteCertificateCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteCertificateCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Certificates.FindAsync(new object[] { request.Id }, cancellationToken);
        Guard.Against.NotFound(request.Id, entity);

        _context.Certificates.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class DeleteCertificateCommandValidator : AbstractValidator<DeleteCertificateCommand>
{
    public DeleteCertificateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
