namespace AskAMuslimBackend.Domain.Entities;

public class Student : BaseEntity
{
    public Guid UserId { get; set; }
    public DateOnly EnrollmentDate { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public StudentProfile? Profile { get; set; }
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<StudentNote> StudentNotes { get; set; } = new List<StudentNote>();
    public ICollection<StudentQuestion> StudentQuestions { get; set; } = new List<StudentQuestion>();
    public ICollection<LessonFeedback> LessonFeedbacks { get; set; } = new List<LessonFeedback>();
    public ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
    public ICollection<AdminNote> AdminNotes { get; set; } = new List<AdminNote>();
}
