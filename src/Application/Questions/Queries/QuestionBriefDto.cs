using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.Questions.Queries;

public class QuestionBriefDto
{
    public Guid Id { get; init; }
    public string Text { get; init; } = string.Empty;
    public int OptionsCount { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Question, QuestionBriefDto>()
                .ForMember(d => d.OptionsCount, opt => opt.MapFrom(s => s.Options.Count));
        }
    }
}
