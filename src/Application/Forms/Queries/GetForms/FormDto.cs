using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.Forms.Queries.GetForms;

public class FormDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPublished { get; set; }

    public List<FormFieldDto> Fields { get; set; } = new();
}

public class FormFieldDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public FormFieldType Type { get; set; }
    public bool IsRequired { get; set; }
    public int Order { get; set; }

    public List<FormFieldOptionDto> Options { get; set; } = new();
}

public class FormFieldOptionDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int Order { get; set; }
}