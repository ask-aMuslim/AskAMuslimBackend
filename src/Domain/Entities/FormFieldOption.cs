namespace AskAMuslimBackend.Domain.Entities;

public class FormFieldOption : BaseAuditableEntity
{
    public Guid FormFieldId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int Order { get; set; }

    public FormField? FormField { get; set; }
}