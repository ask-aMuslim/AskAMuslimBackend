using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Certificates.Commands.CreateCertificate;

public record CreateCertificateCommand : IRequest<Result<Guid>>
{
    public Guid StudentId { get; init; }
    public Guid LevelId { get; init; }
    public Guid? CourseId { get; init; }
    public string Url { get; init; } = string.Empty;
}

public class CreateCertificateCommandHandler : IRequestHandler<CreateCertificateCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateCertificateCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateCertificateCommand request, CancellationToken cancellationToken)
    {
        var studentExists = await _context.Students.AnyAsync(s => s.Id == request.StudentId, cancellationToken);
        var levelExists = await _context.Levels.AnyAsync(l => l.Id == request.LevelId, cancellationToken);

        if (!studentExists || !levelExists)
        {
            var errors = new List<string>();
            if (!studentExists) errors.Add("Student does not exist.");
            if (!levelExists) errors.Add("Level does not exist.");
            return Result<Guid>.Failure(errors);
        }

        if (request.CourseId.HasValue)
        {
            var courseExists = await _context.Courses.AnyAsync(co => co.Id == request.CourseId.Value, cancellationToken);
            if (!courseExists)
            {
                return Result<Guid>.Failure(new[] { "Course does not exist." });
            }
        }

        var alreadyIssued = await _context.Certificates.AnyAsync(c => c.StudentId == request.StudentId && c.LevelId == request.LevelId, cancellationToken);
        if (alreadyIssued)
        {
            return Result<Guid>.Failure(new[] { "Certificate already issued for this level to the student." });
        }

        var entity = new Certificate
        {
            StudentId = request.StudentId,
            LevelId = request.LevelId,
            CourseId = request.CourseId,
            Url = request.Url,
            IssuedAt = DateTime.UtcNow
        };

        _context.Certificates.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(entity.Id);
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
