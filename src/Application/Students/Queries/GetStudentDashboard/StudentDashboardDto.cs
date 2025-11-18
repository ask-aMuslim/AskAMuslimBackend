namespace AskAMuslimBackend.Application.Students.Queries.GetStudentDashboard;

public class StudentDashboardDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Country { get; set; }
    public StudentProgressDto Progress { get; set; } = new();
    public bool IsActive { get; set; }
}

public class StudentProgressDto
{
    public string? CurrentLevel { get; set; }
    public string? CurrentCourse { get; set; }
    public string? CurrentLesson { get; set; }
    public decimal AverageExamMarks { get; set; }
    public int CompletedCourses { get; set; }
    public int TotalEnrollments { get; set; }
    public decimal OverallProgress { get; set; }
}
