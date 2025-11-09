using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Certificates.Commands.CreateCertificate;

public record CreateCertificateCommand : IRequest<Guid>
{
    public Guid StudentId { get; init; }
    public Guid LevelId { get; init; }
    public string Url { get; init; } = string.Empty;
}

public class CreateCertificateCommandHandler : IRequestHandler<CreateCertificateCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateCertificateCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateCertificateCommand request, CancellationToken cancellationToken)
    {
        var studentExists = await _context.Students.AnyAsync(s => s.Id == request.StudentId, cancellationToken);
        var levelExists = await _context.Levels.AnyAsync(l => l.Id == request.LevelId, cancellationToken);

        if (!studentExists || !levelExists)
        {
            var failures = new List<FluentValidation.Results.ValidationFailure>();
            if (!studentExists) failures.Add(new("StudentId", "Student does not exist."));
            if (!levelExists) failures.Add(new("LevelId", "Level does not exist."));
            throw new ValidationException(failures);
        }

        var alreadyIssued = await _context.Certificates.AnyAsync(c => c.StudentId == request.StudentId && c.LevelId == request.LevelId, cancellationToken);
        if (alreadyIssued)
        {
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("LevelId", "Certificate already issued for this level to the student.") });
        }

        var entity = new Certificate
        {
            StudentId = request.StudentId,
            LevelId = request.LevelId,
            Url = request.Url,
            IssuedAt = DateTime.UtcNow
        };

        _context.Certificates.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

public class CreateCertificateCommandValidator : AbstractValidator<CreateCertificateCommand>
{
    public CreateCertificateCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.LevelId).NotEmpty();
        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("Url is required.")
            .MaximumLength(500).WithMessage("Url must not exceed 500 characters.");
    }
}
