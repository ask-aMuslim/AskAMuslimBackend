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

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<QATranslation, QATranslationDto>();
        }
    }
}

public class QADetailDto
{
    public Guid Id { get; init; }
    public bool IsPublished { get; init; }
    public List<QATranslationDto> Translations { get; init; } = new();
    public List<Guid> TagIds { get; init; } = new();

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<QA, QADetailDto>()
                .ForMember(d => d.TagIds, opt => opt.MapFrom(s => s.QATags.Select(t => t.TagId)))
                .ForMember(d => d.Translations, opt => opt.MapFrom(s => s.Translations));
        }
    }
}
