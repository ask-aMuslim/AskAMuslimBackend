using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using FluentValidation;
using System.Text.Json.Serialization;

namespace AskAMuslimBackend.Application.Levels.Commands.DeleteLevel;

public record DeleteLevelCommand([property: JsonIgnore] Guid Id) : IRequest<Result>;

public class DeleteLevelCommandHandler : IRequestHandler<DeleteLevelCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteLevelCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteLevelCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Levels.FindAsync(new object[] { request.Id }, cancellationToken);
        if (entity == null)
        {
            return Result.Failure(new[] { "Level not found" });
        }

        // Check if level has any courses
        var hasCourses = await _context.Courses.AnyAsync(c => c.LevelId == request.Id, cancellationToken);
        if (hasCourses)
        {
            return Result.Failure(new[] { "Cannot delete level because it has associated courses. Please remove or reassign the courses first." });
        }

        try
        {
            _context.Levels.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            // Optionally log ex here
            var message = ex.InnerException?.Message ?? ex.Message;
            return Result.Failure(new[] { $"Failed to delete level: {message}" });
        }
    }
}

public class DeleteLevelCommandValidator : AbstractValidator<DeleteLevelCommand>
{
    public DeleteLevelCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Level Id is required.");
    }
}
