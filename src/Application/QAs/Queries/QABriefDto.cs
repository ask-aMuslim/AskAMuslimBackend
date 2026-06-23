using System.Linq.Expressions;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
namespace AskAMuslimBackend.Application.QAs.Queries;

public class QABriefTranslationDto
{
    public Language Language { get; init; }
    public string QuestionText { get; init; } = string.Empty;
    public string AnswerText { get; init; } = string.Empty;
    public string? AnswerTextJson { get; init; }
}

public class QABriefDto
{
    public Guid Id { get; init; }
    public bool IsPublished { get; init; }
    public int Order { get; init; }
    public List<QABriefTranslationDto>? Translations { get; init; }
    public IReadOnlyCollection<string> Tags { get; init; } = [];
}
