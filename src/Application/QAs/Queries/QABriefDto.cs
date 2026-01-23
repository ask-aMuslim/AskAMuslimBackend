using AskAMuslimBackend.Domain.Entities;
using AutoMapper;

namespace AskAMuslimBackend.Application.QAs.Queries;

public class QABriefDto
{
    public Guid Id { get; init; }
    public string QuestionText { get; init; } = string.Empty;
    public string AnswerText { get; init; } = string.Empty;
    public string? QuestionTextJson { get; init; }
    public string? AnswerTextJson { get; init; }
    public IReadOnlyCollection<string> Tags { get; init; } = [];

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<QA, QABriefDto>()
                .ForMember(d => d.QuestionText, opt => opt.Ignore())
                .ForMember(d => d.AnswerText, opt => opt.Ignore())
                .ForMember(d => d.QuestionTextJson, opt => opt.Ignore())
                .ForMember(d => d.AnswerTextJson, opt => opt.Ignore())
                .ForMember(d => d.Tags, opt => opt.Ignore());
        }
    }
}
