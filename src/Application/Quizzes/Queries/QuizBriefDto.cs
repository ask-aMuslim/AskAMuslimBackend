using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.Quizzes.Queries;

public class QuizBriefDto
{
    public Guid Id { get; init; }
    public Guid? LevelId { get; init; }
    public Guid? CourseId { get; init; }
    public Guid? LessonId { get; init; }
    public QuizTargetType TargetType { get; init; }
    public string Title { get; init; } = string.Empty;
    public double TotalMarks { get; init; }
    public int QuestionsCount { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Quiz, QuizBriefDto>()
                .ForMember(d => d.QuestionsCount, opt => opt.MapFrom(s => s.Questions.Count));
        }
    }
}
