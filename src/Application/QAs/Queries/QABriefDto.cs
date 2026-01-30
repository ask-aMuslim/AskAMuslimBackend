using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using AutoMapper;

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
    public List<QABriefTranslationDto>? Translations { get; init; }
    public IReadOnlyCollection<string> Tags { get; init; } = [];

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<QA, QABriefDto>()
                .ForMember(d => d.Translations, opt => opt.Ignore())
                .ForMember(d => d.Tags, opt => opt.Ignore());
        }
    }
}
