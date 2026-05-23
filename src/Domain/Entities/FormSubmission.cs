using System;

namespace AskAMuslimBackend.Domain.Entities;

public class FormSubmission : BaseAuditableEntity
{
    public Guid FormId { get; set; }
    public string? UserId { get; set; }

    // JSON string containing the answers, e.g. { "fieldId1": "answer1", "fieldId2": ["answer2", "answer3"] }
    public string AnswersJson { get; set; } = string.Empty;

    public Form? Form { get; set; }
}
