using AskAMuslimBackend.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AskAMuslimBackend.Application.Certificates.EventHandlers;

public class CourseCompletedDomainEventHandler : INotificationHandler<CourseCompletedDomainEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<CourseCompletedDomainEventHandler> _logger;

    public CourseCompletedDomainEventHandler(IMediator mediator, ILogger<CourseCompletedDomainEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(CourseCompletedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "CourseCompletedDomainEvent received for Enrollment {EnrollmentId} (Student {StudentId}, Course {CourseId}).",
            notification.EnrollmentId,
            notification.StudentId,
            notification.CourseId);

        await _mediator.Publish(new CertificateAutoGenerationRequestedEvent(
            notification.EnrollmentId,
            notification.StudentId,
            notification.CourseId,
            notification.LevelId,
            notification.CompletedAt), cancellationToken);
    }
}
