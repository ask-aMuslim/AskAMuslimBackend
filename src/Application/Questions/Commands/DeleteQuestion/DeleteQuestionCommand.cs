using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using FluentValidation;
using System.Text.Json.Serialization;

namespace AskAMuslimBackend.Application.Questions.Commands.DeleteQuestion;

public record DeleteQuestionCommand([property: JsonIgnore] Guid Id) : IRequest<Result>;

public class DeleteQuestionCommandHandler : IRequestHandler<DeleteQuestionCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ILocalizationService _localizer;

    public DeleteQuestionCommandHandler(IApplicationDbContext context, ILocalizationService localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    public async Task<Result> Handle(DeleteQuestionCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Questions.FindAsync(new object[] { request.Id }, cancellationToken);
        if (entity == null) return Result.Failure(new[] { _localizer["Questions.NotFound"] });

        _context.Questions.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public class DeleteQuestionCommandValidator : AbstractValidator<DeleteQuestionCommand>
{
    public DeleteQuestionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
