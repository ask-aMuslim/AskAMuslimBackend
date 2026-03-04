using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Validators;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace AskAMuslimBackend.Application.QAs.Commands.UpdateQA;

public record UpdateQACommand : IRequest<Result>
{
    [JsonIgnore] // Set from route parameter, not request body
    public Guid Id { get; init; }
    public bool IsPublished { get; init; }
    public List<UpdateQATranslationInput> Translations { get; init; } = new();
    public List<Guid> TagIds { get; init; } = new();
}

public record UpdateQATranslationInput
{
    public Guid? Id { get; init; } // null => new translation
    public Domain.Enums.Language Language { get; init; }
    public string QuestionText { get; init; } = string.Empty;
    public string AnswerText { get; init; } = string.Empty;
    public string? AnswerTextJson { get; init; }
    public bool IsDeleted { get; init; } // mark existing for deletion
}

public class UpdateQACommandHandler : IRequestHandler<UpdateQACommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ILocalizationService _localizer;

    public UpdateQACommandHandler(IApplicationDbContext context, ILocalizationService localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    public async Task<Result> Handle(UpdateQACommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.QAs
            .Include(q => q.Translations)
            .Include(q => q.QATags)
            .FirstOrDefaultAsync(q => q.Id == request.Id, cancellationToken);

        if (entity == null)
            return Result.Failure(new[] { _localizer["QAs.NotFound"] });

        entity.IsPublished = request.IsPublished;

        // Update tags
        var currentTagIds = entity.QATags.Select(t => t.TagId).ToHashSet();
        var desiredTagIds = request.TagIds.Distinct().ToHashSet();

        var toRemove = entity.QATags.Where(t => !desiredTagIds.Contains(t.TagId)).ToList();
        foreach (var rem in toRemove)
            entity.QATags.Remove(rem);

        var toAdd = desiredTagIds.Except(currentTagIds);
        foreach (var tagId in toAdd)
            entity.QATags.Add(new Domain.Entities.QATag { QAId = entity.Id, TagId = tagId });

        // Update translations
        var translationsById = entity.Translations.ToDictionary(t => t.Id, t => t);
        foreach (var tr in request.Translations)
        {
            if (tr.Id.HasValue && translationsById.TryGetValue(tr.Id.Value, out var existing))
            {
                if (tr.IsDeleted)
                {
                    entity.Translations.Remove(existing);
                }
                else
                {
                    existing.Language = tr.Language;
                    existing.QuestionText = tr.QuestionText;
                    existing.AnswerText = tr.AnswerText;
                    existing.AnswerTextJson = tr.AnswerTextJson;
                }
            }
            else if (!tr.IsDeleted && !tr.Id.HasValue)
            {
                entity.Translations.Add(new Domain.Entities.QATranslation
                {
                    Language = tr.Language,
                    QuestionText = tr.QuestionText,
                    AnswerText = tr.AnswerText,
                    AnswerTextJson = tr.AnswerTextJson
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public class UpdateQACommandValidator : AbstractValidator<UpdateQACommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateQACommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.Id).NotEmpty();

        RuleForEach(x => x.TagIds)
            .NotEmpty().WithMessage("Tag Id must not be empty.")
            .MustAsync(TagExists).WithMessage("Tag does not exist.");

        RuleForEach(x => x.Translations).ChildRules(tr =>
        {
            tr.RuleFor(t => t.QuestionText).NotEmpty().MaximumLength(5000);
            tr.RuleFor(t => t.AnswerText).NotEmpty().MaximumLength(20000);
            tr.RuleFor(t => t.AnswerTextJson)
                .Must(BeValidTiptapJson).WithMessage(t => GetTiptapJsonError(t.AnswerTextJson))
                .When(t => !string.IsNullOrEmpty(t.AnswerTextJson));
            tr.RuleFor(t => t.Language).IsInEnum();
        });

        RuleFor(x => x.Translations.Where(t => !t.IsDeleted).Select(t => t.Language))
            .Must(langs => langs.Distinct().Count() == langs.Count())
            .WithMessage("Active translations must have unique languages.");
    }

    private async Task<bool> TagExists(Guid tagId, CancellationToken ct) => await _context.Tags.AnyAsync(t => t.Id == tagId, ct);

    private bool BeValidTiptapJson(string? json)
        => TiptapJsonValidator.IsValidTiptapJson(json, out _);

    private string GetTiptapJsonError(string? json)
    {
        TiptapJsonValidator.IsValidTiptapJson(json, out var error);
        return error ?? "Invalid Tiptap JSON.";
    }
}
