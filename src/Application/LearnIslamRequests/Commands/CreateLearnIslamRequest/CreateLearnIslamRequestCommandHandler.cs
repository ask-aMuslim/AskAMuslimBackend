using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.LearnIslamRequests.Commands.CreateLearnIslamRequest;

public class CreateLearnIslamRequestCommandHandler : IRequestHandler<CreateLearnIslamRequestCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public CreateLearnIslamRequestCommandHandler(IApplicationDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<Result<Guid>> Handle(CreateLearnIslamRequestCommand request, CancellationToken cancellationToken)
    {
        var entity = new LearnIslamRequest
        {
            FullName = request.FullName,
            Email = request.Email,
            Country = request.Country,
            PreferredLanguage = request.PreferredLanguage,
            KnowledgeLevel = request.KnowledgeLevel,
            Message = request.Message,
            CurrentReligion = request.CurrentReligion,
            Status = LearnIslamRequestStatus.Pending
        };

        _context.LearnIslamRequests.Add(entity);

        await _context.SaveChangesAsync(cancellationToken);

        // Send email to admins
        var adminUserIds = await _context.Admins
            .Select(a => a.UserId)
            .ToListAsync(cancellationToken);

        var admins = await _context.Users
            .Where(u => adminUserIds.Contains(u.Id))
            .Select(u => u.Email)
            .Where(e => !string.IsNullOrEmpty(e))
            .ToArrayAsync(cancellationToken);

        if (admins.Any())
        {
            var subject = $"New Learn Islam Request: {request.FullName}";
            var body = $@"
                <h2>New Learn Islam Form Submission</h2>
                <p><strong>Name:</strong> {request.FullName}</p>
                <p><strong>Email:</strong> {request.Email}</p>
                <p><strong>Country:</strong> {request.Country}</p>
                <p><strong>Language:</strong> {request.PreferredLanguage}</p>
                <p><strong>Current Religion:</strong> {request.CurrentReligion ?? "Not specified"}</p>
                <p><strong>Knowledge Level:</strong> {request.KnowledgeLevel}</p>
                <p><strong>Message:</strong></p>
                <p>{request.Message ?? "No message provided."}</p>
            ";

            await _emailService.SendEmailAsync(admins!, subject, body, true);
        }

        return Result<Guid>.Success(entity.Id);
    }
}