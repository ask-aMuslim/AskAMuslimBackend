using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.QAs.Queries;

public class QABriefDto
{
    public Guid Id { get; init; }
    public string? ImageUrl { get; init; }
    public string? VideoUrl { get; init; }
    public bool IsPublished { get; init; }
    public int TranslationsCount { get; init; }
    public int TagsCount { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<QA, QABriefDto>()
                .ForMember(d => d.TranslationsCount, opt => opt.MapFrom(s => s.Translations.Count))
                .ForMember(d => d.TagsCount, opt => opt.MapFrom(s => s.QATags.Count));
        }
    }
}
