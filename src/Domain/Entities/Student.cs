namespace AskAMuslimBackend.Domain.Entities;

public class Student : BaseEntity
{
    public Guid UserId { get; set; }
    public string? Bio { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<StudentNote> StudentNotes { get; set; } = new List<StudentNote>();
    public ICollection<StudentQuestion> StudentQuestions { get; set; } = new List<StudentQuestion>();
    public ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
}
