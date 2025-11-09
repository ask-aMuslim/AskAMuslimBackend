using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;

namespace AskAMuslimBackend.Application.Enrollments.Commands.UpdateEnrollmentStatus;

public record UpdateEnrollmentStatusCommand : IRequest
{
    public Guid Id { get; init; }
    public EnrollmentStatus Status { get; init; }
}

public class UpdateEnrollmentStatusCommandHandler : IRequestHandler<UpdateEnrollmentStatusCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateEnrollmentStatusCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateEnrollmentStatusCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Enrollments.FindAsync(new object[] { request.Id }, cancellationToken);
        Guard.Against.NotFound(request.Id, entity);

        entity.Status = request.Status;
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class UpdateEnrollmentStatusCommandValidator : AbstractValidator<UpdateEnrollmentStatusCommand>
{
    public UpdateEnrollmentStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
    }
}
