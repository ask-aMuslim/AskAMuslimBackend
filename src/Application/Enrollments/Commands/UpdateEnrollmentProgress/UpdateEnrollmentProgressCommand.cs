using AskAMuslimBackend.Application.Common.Interfaces;
using FluentValidation;

namespace AskAMuslimBackend.Application.Enrollments.Commands.UpdateEnrollmentProgress;

public record UpdateEnrollmentProgressCommand : IRequest
{
    public Guid Id { get; init; }
    public double LevelProgress { get; init; }
    public double CourseProgress { get; init; }
}

public class UpdateEnrollmentProgressCommandHandler : IRequestHandler<UpdateEnrollmentProgressCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateEnrollmentProgressCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateEnrollmentProgressCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Enrollments.FindAsync(new object[] { request.Id }, cancellationToken);
        Guard.Against.NotFound(request.Id, entity);

        entity.LevelProgress = request.LevelProgress;
        entity.CourseProgress = request.CourseProgress;
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class UpdateEnrollmentProgressCommandValidator : AbstractValidator<UpdateEnrollmentProgressCommand>
{
    public UpdateEnrollmentProgressCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.LevelProgress).InclusiveBetween(0, 100);
        RuleFor(x => x.CourseProgress).InclusiveBetween(0, 100);
    }
}
