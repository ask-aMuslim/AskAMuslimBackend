using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Notifications.Commands.SendNotification;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AskAMuslimBackend.Application.Certificates.EventHandlers;

public class CertificateAutoGenerationRequestedEventHandler : INotificationHandler<CertificateAutoGenerationRequestedEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;
    private readonly ILogger<CertificateAutoGenerationRequestedEventHandler> _logger;

    public CertificateAutoGenerationRequestedEventHandler(
        IApplicationDbContext context,
        ISender sender,
        ILogger<CertificateAutoGenerationRequestedEventHandler> logger)
    {
        _context = context;
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(CertificateAutoGenerationRequestedEvent notification, CancellationToken cancellationToken)
    {
        // Idempotency: one certificate per (StudentId, CourseId).
        // If one already exists, do nothing. This protects against:
        //   - the same completion event being re-dispatched (interceptor retries)
        //   - an admin manually issuing the cert earlier
        var alreadyIssued = await _context.Certificates
            .AnyAsync(c => c.StudentId == notification.StudentId && c.CourseId == notification.CourseId, cancellationToken);

        if (alreadyIssued)
        {
            _logger.LogInformation(
                "Certificate already exists for Student {StudentId} on Course {CourseId}. Skipping auto-generation.",
                notification.StudentId,
                notification.CourseId);
            return;
        }

        // Resolve student -> user (for notification routing) and course title (for the message).
        var studentUser = await _context.Students
            .Where(s => s.Id == notification.StudentId)
            .Select(s => new { s.UserId })
            .FirstOrDefaultAsync(cancellationToken);

        if (studentUser is null)
        {
            _logger.LogWarning(
                "Certificate auto-generation skipped: Student {StudentId} not found.",
                notification.StudentId);
            return;
        }

        var courseTitle = await _context.Courses
            .Where(c => c.Id == notification.CourseId)
            .Select(c => c.Title)
            .FirstOrDefaultAsync(cancellationToken);

        // Create the certificate row. Url is a deterministic public endpoint that
        // the existing Certificates endpoint already serves (GET /api/Certificates/{id}).
        // If you later add a PDF generator, replace this with the stored file URL.
        var certificate = new Certificate
        {
            StudentId = notification.StudentId,
            LevelId = notification.LevelId,
            CourseId = notification.CourseId,
            Url = $"/api/Certificates/{Guid.NewGuid()}", // placeholder; updated below after save
            IssuedAt = notification.CompletedAt
        };

        _context.Certificates.Add(certificate);
        await _context.SaveChangesAsync(cancellationToken);

        certificate.Url = $"/api/Certificates/{certificate.Id}";
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Certificate {CertificateId} auto-generated for Student {StudentId} on Course {CourseId}.",
            certificate.Id,
            notification.StudentId,
            notification.CourseId);

        // Notify the student (in-app + FCM push + email). Failures inside SendNotification are logged
        // and swallowed, so a notification hiccup never blocks certificate issuance.
        await _sender.Send(new SendNotificationCommand
        {
            UserId = studentUser.UserId,
            Title = "Course completed!",
            Message = $"Congratulations! You completed \"{courseTitle ?? "the course"}\". Your certificate is now available.",
            ActionUrl = certificate.Url,
            CustomData = new Dictionary<string, string>
            {
                ["certificateId"] = certificate.Id.ToString(),
                ["courseId"] = notification.CourseId.ToString()
            }
        }, cancellationToken);
    }
}
