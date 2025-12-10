using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using FluentValidation;

namespace AskAMuslimBackend.Application.Certificates.Commands.DeleteCertificate;

public record DeleteCertificateCommand(Guid Id) : IRequest<Result>;

public class DeleteCertificateCommandHandler : IRequestHandler<DeleteCertificateCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteCertificateCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteCertificateCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Certificates.FindAsync(new object[] { request.Id }, cancellationToken);
        
        if (entity == null)
        {
            return Result.Failure(new[] { "Certificate not found" });
        }

        _context.Certificates.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
    }
}

public class DeleteCertificateCommandValidator : AbstractValidator<DeleteCertificateCommand>
{
    public DeleteCertificateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
