using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.StudentNotes.Queries;

public class StudentNoteDto
{
    public Guid Id { get; init; }
    public Guid StudentId { get; init; }
    public Guid LessonId { get; init; }
    public string Text { get; init; } = string.Empty;
    public double Timestamp { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<StudentNote, StudentNoteDto>();
        }
    }
}
