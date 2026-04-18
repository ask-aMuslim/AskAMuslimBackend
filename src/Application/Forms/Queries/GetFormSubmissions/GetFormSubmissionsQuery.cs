using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.Forms.Queries.GetFormSubmissions;       

public class FormSubmissionDto
{
    public Guid Id { get; set; }
    public Guid FormId { get; set; }
    public string? UserId { get; set; }
    public string AnswersJson { get; set; } = string.Empty;
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
        var submissions = await _context.FormSubmissions
            .Where(fs => fs.FormId == request.FormId)
            .Select(fs => new FormSubmissionDto
            {
                Id = fs.Id,
                FormId = fs.FormId,
                UserId = fs.UserId,
                AnswersJson = fs.AnswersJson,
                Created = fs.Created
            })
            .ToListAsync(cancellationToken);

        return Result<List<FormSubmissionDto>>.Success(submissions);
    }
}
