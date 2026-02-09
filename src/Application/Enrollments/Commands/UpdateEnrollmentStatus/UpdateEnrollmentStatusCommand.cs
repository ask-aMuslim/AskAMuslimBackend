using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;
using System.Text.Json.Serialization;

namespace AskAMuslimBackend.Application.Enrollments.Commands.UpdateEnrollmentStatus;

public record UpdateEnrollmentStatusCommand : IRequest<Result>
{
    [JsonIgnore] // Set from route parameter, not request body
    public Guid Id { get; init; }
    public EnrollmentStatus Status { get; init; }
}

public class UpdateEnrollmentStatusCommandHandler : IRequestHandler<UpdateEnrollmentStatusCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateEnrollmentStatusCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UpdateEnrollmentStatusCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Enrollments.FindAsync(new object[] { request.Id }, cancellationToken);
        
        if (entity == null)
        {
            return Result.Failure(new[] { $"Enrollment with id {request.Id} not found" });
        }

        entity.Status = request.Status;
        await _context.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
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
