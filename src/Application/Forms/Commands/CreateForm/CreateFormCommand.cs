using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.Forms.Commands.CreateForm;

public record CreateFormFieldCommand
{
    public string Label { get; init; } = string.Empty;
    public FormFieldType Type { get; init; } = FormFieldType.Text;
    public bool IsRequired { get; init; }
    public int Order { get; init; }
    public List<CreateFormFieldOptionCommand> Options { get; init; } = new();
}

public record CreateFormFieldOptionCommand
{
    public string Label { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public int Order { get; init; }
}

public record CreateFormCommand : IRequest<Result<Guid>>
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsPublished { get; init; }
    public List<CreateFormFieldCommand> Fields { get; init; } = new();
}

public class CreateFormCommandHandler : IRequestHandler<CreateFormCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateFormCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateFormCommand request, CancellationToken cancellationToken)
    {
        var form = new Form
        {
            Title = request.Title,
            Description = request.Description,
            IsPublished = request.IsPublished
        };

        foreach (var field in request.Fields)
        {
            var formField = new FormField
            {
                Label = field.Label,
                Type = field.Type,
                IsRequired = field.IsRequired,
                Order = field.Order,
                Form = form
            };

            foreach (var opt in field.Options)
            {
                formField.Options.Add(new FormFieldOption
                {
                    Label = opt.Label,
                    Value = opt.Value,
                    Order = opt.Order,
                    FormField = formField
                });
            }

            form.Fields.Add(formField);
        }

        _context.Forms.Add(form);
        await _context.SaveChangesAsync(cancellationToken);
        
        return Result<Guid>.Success(form.Id);
    }
}