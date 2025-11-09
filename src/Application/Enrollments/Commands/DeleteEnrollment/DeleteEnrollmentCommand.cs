using AskAMuslimBackend.Application.Common.Interfaces;
using FluentValidation;

namespace AskAMuslimBackend.Application.Enrollments.Commands.DeleteEnrollment;

public record DeleteEnrollmentCommand(Guid Id) : IRequest;

public class DeleteEnrollmentCommandHandler : IRequestHandler<DeleteEnrollmentCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteEnrollmentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteEnrollmentCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Enrollments.FindAsync(new object[] { request.Id }, cancellationToken);
        Guard.Against.NotFound(request.Id, entity);

        _context.Enrollments.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class DeleteEnrollmentCommandValidator : AbstractValidator<DeleteEnrollmentCommand>
{
    public DeleteEnrollmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
