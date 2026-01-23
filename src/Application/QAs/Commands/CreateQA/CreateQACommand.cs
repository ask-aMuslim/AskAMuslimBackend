using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Validators;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.QAs.Commands.CreateQA;

public record QATranslationInput
{
    public Language Language { get; init; }
    public string QuestionText { get; init; } = string.Empty;
    public string AnswerText { get; init; } = string.Empty;
    public string? AnswerTextJson { get; init; }
}

public record CreateQACommand : IRequest<Result<Guid>>
{
    public string? ImageUrl { get; init; }
    public string? VideoUrl { get; init; }
    public bool IsPublished { get; init; }
    public List<QATranslationInput> Translations { get; init; } = new();
    public List<Guid> TagIds { get; init; } = new();
}

public class CreateQACommandHandler : IRequestHandler<CreateQACommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateQACommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateQACommand request, CancellationToken cancellationToken)
    {
        // Validate Tag existence defensively (in addition to validator MustAsync) to surface all missing tags
        if (request.TagIds.Any())
        {
            var existingTagIds = await _context.Tags.AsNoTracking().Where(t => request.TagIds.Contains(t.Id)).Select(t => t.Id).ToListAsync(cancellationToken);
            var missing = request.TagIds.Distinct().Except(existingTagIds).ToList();
            if (missing.Any())
            {
                return Result<Guid>.Failure(missing.Select(id => $"Tag {id} does not exist.").ToArray());
            }
        }

        // Ensure unique languages in request
        var duplicateLangs = request.Translations.GroupBy(t => t.Language).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicateLangs.Any())
        {
            return Result<Guid>.Failure(duplicateLangs.Select(l => $"Duplicate language {l}.").ToArray());
        }

        var entity = new QA
        {
            ImageUrl = request.ImageUrl,
            VideoUrl = request.VideoUrl,
            IsPublished = request.IsPublished,
        };

        foreach (var t in request.Translations)
        {
            entity.Translations.Add(new QATranslation
            {
                Language = t.Language,
                QuestionText = t.QuestionText,
                AnswerText = t.AnswerText,
                AnswerTextJson = t.AnswerTextJson
            });
        }

        foreach (var tagId in request.TagIds.Distinct())
        {
            entity.QATags.Add(new QATag { TagId = tagId, QA = entity });
        }

        _context.QAs.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(entity.Id);
    }
}

public class CreateQACommandValidator : AbstractValidator<CreateQACommand>
{
    private readonly IApplicationDbContext _context;

    public CreateQACommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleForEach(x => x.TagIds)
            .NotEmpty().WithMessage("Tag Id must not be empty.")
            .MustAsync(TagExists).WithMessage("Tag does not exist.");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(2048).When(x => !string.IsNullOrEmpty(x.ImageUrl));
        RuleFor(x => x.VideoUrl)
            .MaximumLength(2048).When(x => !string.IsNullOrEmpty(x.VideoUrl));

        RuleFor(x => x.Translations)
            .NotEmpty().WithMessage("At least one translation is required.");

        RuleForEach(x => x.Translations).ChildRules(tr =>
        {
            tr.RuleFor(t => t.QuestionText).NotEmpty().MaximumLength(5000);
            tr.RuleFor(t => t.AnswerText).NotEmpty().MaximumLength(20000);
            tr.RuleFor(t => t.AnswerTextJson)
                .Must(BeValidTiptapJson).WithMessage(t => GetTiptapJsonError(t.AnswerTextJson))
                .When(t => !string.IsNullOrEmpty(t.AnswerTextJson));
            tr.RuleFor(t => t.Language).IsInEnum();
        });

        RuleFor(x => x.Translations.Select(t => t.Language))
            .Must(langs => langs.Distinct().Count() == langs.Count())
            .WithMessage("Translations must have unique languages.");
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
