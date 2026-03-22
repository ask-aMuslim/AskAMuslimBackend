using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AskAMuslimBackend.Application.Volunteers.Commands.CreateVolunteer;

public class CreateVolunteerCommandHandler : IRequestHandler<CreateVolunteerCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public CreateVolunteerCommandHandler(IApplicationDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<Result<Guid>> Handle(CreateVolunteerCommand request, CancellationToken cancellationToken)
    {
        var entity = new Volunteer
        {
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            Type = request.Type,
            Message = request.Message
        };

        _context.Volunteers.Add(entity);

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
            var subject = $"New Volunteer Application: {request.Type}";
            var body = $@"
                <h2>New Volunteer Application</h2>
                <p><strong>Name:</strong> {request.Name}</p>
                <p><strong>Email:</strong> {request.Email}</p>
                <p><strong>Phone:</strong> {request.Phone}</p>
                <p><strong>Volunteering Type:</strong> {request.Type}</p>
                <p><strong>Message:</strong></p>
                <p>{request.Message}</p>
            ";

            await _emailService.SendEmailAsync(admins!, subject, body, true);
        }

        return Result<Guid>.Success(entity.Id);
    }
}