using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using FluentValidation;
using System.Text.Json.Serialization;

namespace AskAMuslimBackend.Application.Enrollments.Commands.DeleteEnrollment;

public record DeleteEnrollmentCommand([property: JsonIgnore] Guid Id) : IRequest<Result>;

public class DeleteEnrollmentCommandHandler : IRequestHandler<DeleteEnrollmentCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteEnrollmentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteEnrollmentCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Enrollments.FindAsync(new object[] { request.Id }, cancellationToken);
        
        if (entity == null)
        {
            return Result.Failure(new[] { $"Enrollment with id {request.Id} not found" });
        }

        _context.Enrollments.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
    }
}

public class DeleteEnrollmentCommandValidator : AbstractValidator<DeleteEnrollmentCommand>
{
    public DeleteEnrollmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
