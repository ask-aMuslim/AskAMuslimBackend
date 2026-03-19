using System.Linq.Expressions;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.Enrollments.Queries;

public class EnrollmentDto
{
    public Guid Id { get; init; }
    public Guid StudentId { get; init; }
    public Guid CourseId { get; init; }
    public DateTime EnrolledAt { get; init; }
    public EnrollmentStatus Status { get; init; }
    public double LevelProgress { get; init; }
    public double CourseProgress { get; init; }
}
