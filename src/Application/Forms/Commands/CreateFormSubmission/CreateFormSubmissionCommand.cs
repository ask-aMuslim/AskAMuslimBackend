using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace AskAMuslimBackend.Application.Forms.Commands.CreateFormSubmission;

public record CreateFormSubmissionCommand : IRequest<Result<Guid>>
{
    public Guid FormId { get; init; }
    public string AnswersJson { get; init; } = string.Empty;
    public List<IFormFile>? Files { get; init; }
}

public class CreateFormSubmissionCommandHandler : IRequestHandler<CreateFormSubmissionCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IFileStorageService _fileStorageService;

    public CreateFormSubmissionCommandHandler(
        IApplicationDbContext context, 
        IUser user, 
        IFileStorageService fileStorageService)
    {
        _context = context;
        _user = user;
        _fileStorageService = fileStorageService;
    }

    public async Task<Result<Guid>> Handle(CreateFormSubmissionCommand request, CancellationToken cancellationToken)
    {
        var form = await _context.Forms.FindAsync(new object[] { request.FormId }, cancellationToken);
        
        if (form == null)
        {
            return Result<Guid>.Failure(new[] { "Form not found." });
        }

        var answersJson = request.AnswersJson;

        if (request.Files != null && request.Files.Any())
        {
            foreach (var file in request.Files)
            {
                var fileUrl = await _fileStorageService.UploadFileAsync(file, "form-attachments", cancellationToken);
                answersJson = answersJson.Replace(file.FileName, fileUrl);
            }
        }

        var submission = new FormSubmission
        {
            FormId = request.FormId,
            UserId = _user.Id, // can be null if anonymous is allowed
            AnswersJson = answersJson,
            Form = form
        };

        _context.FormSubmissions.Add(submission);
        await _context.SaveChangesAsync(cancellationToken);
        
        return Result<Guid>.Success(submission.Id);
    }
}