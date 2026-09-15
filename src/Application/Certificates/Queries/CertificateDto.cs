using System.Linq.Expressions;
using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.Certificates.Queries;

public class CertificateDto
{
    public Guid Id { get; init; }
    public Guid StudentId { get; init; }
    public Guid LevelId { get; init; }
    public Guid? CourseId { get; init; }
    public string Url { get; init; } = string.Empty;
    public DateTime IssuedAt { get; init; }

    public string? StudentFullName { get; init; }
    public string? LevelName { get; init; }
    public string? CourseName { get; init; }
}
