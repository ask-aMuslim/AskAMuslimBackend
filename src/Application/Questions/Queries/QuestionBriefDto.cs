using System.Linq.Expressions;
using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.Questions.Queries;

public class QuestionBriefDto
{
    public Guid Id { get; init; }
    public string Text { get; init; } = string.Empty;
    public int Order { get; init; }
    public int OptionsCount { get; init; }
}
