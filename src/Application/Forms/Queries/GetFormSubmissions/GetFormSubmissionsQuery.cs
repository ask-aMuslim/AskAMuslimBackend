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
    public List<string> Values { get; set; } = [];
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
                var fieldMetadata = fieldMapping.TryGetValue(fieldId, out var metadata) ? metadata : null;
                var (value, values) = ExtractAnswerValue(property.Value);

                answers.Add(new FormAnswerDto
                {
                    Type = fieldMetadata?.Type ?? FormFieldType.Text,
                    FieldName = fieldMetadata?.Label ?? fieldId,
                    Value = value,
                    Values = values
                });
            }

            return answers;
        }
        catch
        {
            return [];
        }
    }

    private static (string Value, List<string> Values) ExtractAnswerValue(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            var values = new List<string>();

            foreach (var item in element.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    values.Add(item.GetString() ?? string.Empty);
                    continue;
                }

                if (item.ValueKind == JsonValueKind.Null || item.ValueKind == JsonValueKind.Undefined)
                {
                    values.Add(string.Empty);
                    continue;
                }

                values.Add(item.ToString());
            }

            var joinedValue = values.Count > 0 ? string.Join(", ", values) : string.Empty;
            return (joinedValue, values);
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            return (element.GetString() ?? string.Empty, []);
        }

        if (element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
        {
            return (string.Empty, []);
        }

        return (element.ToString(), []);
    }
}
