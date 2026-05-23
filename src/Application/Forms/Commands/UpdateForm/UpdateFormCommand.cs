using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Forms.Commands.UpdateForm;

public record UpdateFormFieldCommand
{
    public Guid? Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public FormFieldType Type { get; init; } = FormFieldType.Text;
    public bool IsMultiSelect { get; init; }
    public bool IsRequired { get; init; }
    public int Order { get; init; }
    public List<UpdateFormFieldOptionCommand> Options { get; init; } = new();
}

public record UpdateFormFieldOptionCommand
{
    public Guid? Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public int Order { get; init; }
}

public record UpdateFormCommand : IRequest<Result<Guid>>
{
    public Guid Id { get; set; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsPublished { get; init; }
    public List<UpdateFormFieldCommand> Fields { get; init; } = new();
}

public class UpdateFormCommandHandler : IRequestHandler<UpdateFormCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public UpdateFormCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(UpdateFormCommand request, CancellationToken cancellationToken)
    {
        var form = await _context.Forms
            .Include(f => f.Fields)
                .ThenInclude(f => f.Options)
            .FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken);

        if (form == null)
        {
            return Result<Guid>.Failure(new[] { $"Form with ID {request.Id} not found." });
        }

        form.Title = request.Title;
        form.Description = request.Description;
        form.IsPublished = request.IsPublished;

        // Process fields to remove
        var incomingFieldIds = request.Fields.Where(f => f.Id.HasValue).Select(f => f.Id!.Value).ToList();
        var fieldsToRemove = form.Fields.Where(f => !incomingFieldIds.Contains(f.Id)).ToList();

        foreach (var field in fieldsToRemove)
        {
            form.Fields.Remove(field);
            _context.FormFields.Remove(field);
        }

        // Process fields to update or add
        foreach (var fieldCommand in request.Fields)
        {
            if (fieldCommand.Id.HasValue)
            {
                var existingField = form.Fields.FirstOrDefault(f => f.Id == fieldCommand.Id.Value);
                if (existingField != null)
                {
                    existingField.Label = fieldCommand.Label;
                    existingField.Type = fieldCommand.Type;
                    existingField.IsMultiSelect = fieldCommand.IsMultiSelect;
                    existingField.IsRequired = fieldCommand.IsRequired;
                    existingField.Order = fieldCommand.Order;

                    var incomingOptionIds = fieldCommand.Options.Where(o => o.Id.HasValue).Select(o => o.Id!.Value).ToList();
                    var optionsToRemove = existingField.Options.Where(o => !incomingOptionIds.Contains(o.Id)).ToList();

                    foreach (var option in optionsToRemove)
                    {
                        existingField.Options.Remove(option);
                        _context.FormFieldOptions.Remove(option);
                    }

                    foreach (var optionCommand in fieldCommand.Options)
                    {
                        if (optionCommand.Id.HasValue)
                        {
                            var existingOption = existingField.Options.FirstOrDefault(o => o.Id == optionCommand.Id.Value);
                            if (existingOption != null)
                            {
                                existingOption.Label = optionCommand.Label;
                                existingOption.Value = optionCommand.Value;
                                existingOption.Order = optionCommand.Order;
                                continue;
                            }
                        }

                        // Add new option to existing field
                        existingField.Options.Add(new FormFieldOption
                        {
                            Label = optionCommand.Label,
                            Value = optionCommand.Value,
                            Order = optionCommand.Order,
                            FormField = existingField
                        });
                    }
                    continue;
                }
            }

            // Add new field
            var newField = new FormField
            {
                Label = fieldCommand.Label,
                Type = fieldCommand.Type,
                IsMultiSelect = fieldCommand.IsMultiSelect,
                IsRequired = fieldCommand.IsRequired,
                Order = fieldCommand.Order,
                Form = form
            };

            foreach (var optionCommand in fieldCommand.Options)
            {
                newField.Options.Add(new FormFieldOption
                {
                    Label = optionCommand.Label,
                    Value = optionCommand.Value,
                    Order = optionCommand.Order,
                    FormField = newField
                });
            }

            form.Fields.Add(newField);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(form.Id);
    }
}
