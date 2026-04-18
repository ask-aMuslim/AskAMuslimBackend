using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.Forms.Commands.CreateFormSubmission;

public record CreateFormSubmissionCommand : IRequest<Result<Guid>>
{
    public Guid FormId { get; init; }
    public string AnswersJson { get; init; } = string.Empty;
}

public class CreateFormSubmissionCommandHandler : IRequestHandler<CreateFormSubmissionCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public CreateFormSubmissionCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Result<Guid>> Handle(CreateFormSubmissionCommand request, CancellationToken cancellationToken)
    {
        var form = await _context.Forms.FindAsync(new object[] { request.FormId }, cancellationToken);
        
        if (form == null)
        {
            return Result<Guid>.Failure(new[] { "Form not found." });
        }

        var submission = new FormSubmission
        {
            FormId = request.FormId,
            UserId = _user.Id, // can be null if anonymous is allowed
            AnswersJson = request.AnswersJson,
            Form = form
        };

        _context.FormSubmissions.Add(submission);
        await _context.SaveChangesAsync(cancellationToken);
        
        return Result<Guid>.Success(submission.Id);
    }
}