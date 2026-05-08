using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AskAMuslimBackend.Application.Forms.Queries.GetFormSubmissions;

public class FormFieldMetadataDto
{
    public string Label { get; set; } = string.Empty;
    public FormFieldType Type { get; set; } = FormFieldType.Text;
}

public class FormAnswerDto
{
    public FormFieldType Type { get; set; } = FormFieldType.Text;
    public string FieldName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class FormSubmissionDto
{
    public Guid Id { get; set; }
    public Guid FormId { get; set; }
    public string? UserId { get; set; }
    public List<FormAnswerDto> Answers { get; set; } = [];
    public DateTimeOffset Created { get; set; }
}

public record GetFormSubmissionsQuery(Guid FormId) : IRequest<Result<List<FormSubmissionDto>>>;

public class GetFormSubmissionsQueryHandler : IRequestHandler<GetFormSubmissionsQuery, Result<List<FormSubmissionDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetFormSubmissionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<FormSubmissionDto>>> Handle(GetFormSubmissionsQuery request, CancellationToken cancellationToken)
    {
        // Load form with all fields
        var form = await _context.Forms
            .Include(f => f.Fields)
            .FirstOrDefaultAsync(f => f.Id == request.FormId, cancellationToken);

        if (form is null)
        {
            return Result<List<FormSubmissionDto>>.Failure(["Form not found"]);
        }

        // Create a mapping of field ID to field metadata
        var fieldMapping = form.Fields.ToDictionary(
            f => f.Id.ToString(),
            f => new FormFieldMetadataDto
            {
                Label = f.Label,
                Type = f.Type
            });

        // Load submissions
        var submissions = await _context.FormSubmissions
            .Where(fs => fs.FormId == request.FormId)
            .ToListAsync(cancellationToken);

        // Map submissions to DTOs with field names
        var submissionDtos = submissions.Select(fs =>
        {
            var answers = ParseAnswers(fs.AnswersJson, fieldMapping);
            return new FormSubmissionDto
            {
                Id = fs.Id,
                FormId = fs.FormId,
                UserId = fs.UserId,
                Answers = answers,
                Created = fs.Created
            };
        }).ToList();

        return Result<List<FormSubmissionDto>>.Success(submissionDtos);
    }

    private static List<FormAnswerDto> ParseAnswers(string answersJson, Dictionary<string, FormFieldMetadataDto> fieldMapping)
    {
        if (string.IsNullOrWhiteSpace(answersJson))
        {
            return [];
        }

        try
        {
            var answers = new List<FormAnswerDto>();
            using var doc = JsonDocument.Parse(answersJson);

            foreach (var property in doc.RootElement.EnumerateObject())
            {
                var fieldId = property.Name;
                var value = property.Value.GetString() ?? string.Empty;
                var fieldMetadata = fieldMapping.TryGetValue(fieldId, out var metadata) ? metadata : null;

                answers.Add(new FormAnswerDto
                {
                    Type = fieldMetadata?.Type ?? FormFieldType.Text,
                    FieldName = fieldMetadata?.Label ?? fieldId,
                    Value = value
                });
            }

            return answers;
        }
        catch
        {
            return [];
        }
    }
}
