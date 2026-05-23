using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Domain.Entities;

public class FormField : BaseAuditableEntity
{
    public Guid FormId { get; set; }
    public string Label { get; set; } = string.Empty;
    public FormFieldType Type { get; set; } = FormFieldType.Text;
    public bool IsMultiSelect { get; set; }
    public bool IsRequired { get; set; }
    public int Order { get; set; }

    public ICollection<FormFieldOption> Options { get; set; } = new HashSet<FormFieldOption>();

    public Form? Form { get; set; }
}
