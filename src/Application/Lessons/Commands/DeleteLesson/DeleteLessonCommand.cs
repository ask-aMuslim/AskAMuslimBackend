using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using System.Text.Json.Serialization;

namespace AskAMuslimBackend.Application.Lessons.Commands.DeleteLesson;

public record DeleteLessonCommand([property: JsonIgnore] Guid Id) : IRequest<Result>;

public class DeleteLessonCommandHandler : IRequestHandler<DeleteLessonCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ILocalizationService _localizer;

    public DeleteLessonCommandHandler(IApplicationDbContext context, ILocalizationService localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    public async Task<Result> Handle(DeleteLessonCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Lessons.FindAsync(new object[] { request.Id }, cancellationToken);
        
        if (entity == null)
            return Result.Failure(new[] { _localizer["Lessons.NotFound"] });
        
        _context.Lessons.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
public class DeleteLessonCommandValidator : AbstractValidator<DeleteLessonCommand>
{
    public DeleteLessonCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Lesson Id is required.");
    }
}
