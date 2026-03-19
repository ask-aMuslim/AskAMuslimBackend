using System.Linq.Expressions;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.QAs.Queries;

public class QATranslationDto
{
    public Guid Id { get; init; }
    public Language Language { get; init; }
    public string QuestionText { get; init; } = string.Empty;
    public string AnswerText { get; init; } = string.Empty;
    public string? AnswerTextJson { get; init; }
}

public class QADetailDto
{
    public Guid Id { get; init; }
    public bool IsPublished { get; init; }
    public List<QATranslationDto> Translations { get; init; } = new();
    public List<Guid> TagIds { get; init; } = new();
}
