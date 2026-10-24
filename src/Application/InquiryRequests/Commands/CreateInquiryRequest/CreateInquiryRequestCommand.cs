using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.InquiryRequests.Commands.CreateInquiryRequest;

public record CreateInquiryRequestCommand : IRequest<Result<Guid>>
{
    public MeetingInquiryTopic? Topic { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<Language> Languages { get; init; } = new();
}

public class CreateInquiryRequestCommandHandler : IRequestHandler<CreateInquiryRequestCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IEmailService _emailService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private const string CONTACT_EMAIL = "info@ask-a-muslim.com";

    public CreateInquiryRequestCommandHandler(
        IApplicationDbContext context,
        IUser user,
        IEmailService emailService,
        IBackgroundJobClient backgroundJobClient)
    {
        _context = context;
        _user = user;
        _emailService = emailService;
        _backgroundJobClient = backgroundJobClient;
    }

    public async Task<Result<Guid>> Handle(CreateInquiryRequestCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_user.Id))
        {
            return Result<Guid>.Failure(new[] { "Unauthorized" });
        }

        var requesterGuid = Guid.Parse(_user.Id);

        var inquiry = new InquiryRequest
        {
            RequesterId = requesterGuid,
            Topic = request.Topic ?? MeetingInquiryTopic.GeneralInquiry,
            Message = request.Message,
            Languages = request.Languages,
            Status = MeetingInquiryRequestStatus.Pending,
        };

        _context.InquiryRequests.Add(inquiry);
        await _context.SaveChangesAsync(cancellationToken);

        // Fetch requester details for the notification email
        var requester = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == requesterGuid)
            .Select(u => new { u.FirstName, u.LastName, u.Email })
            .FirstOrDefaultAsync(cancellationToken);

        if (requester != null && !string.IsNullOrEmpty(requester.Email))
        {
            var topicDisplay = request.Topic.HasValue ? request.Topic.Value.ToString() : "General Inquiry";
            var languages = string.Join(", ", request.Languages);

            var subject = $"New Inquiry Request - {topicDisplay}";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 5px;'>
                        <h2 style='color: #2c3e50; border-bottom: 2px solid #3498db; padding-bottom: 10px;'>New Inquiry Request</h2>
                        
                        <div style='margin: 20px 0;'>
                            <p style='margin: 10px 0;'><strong>Requester:</strong> {requester.FirstName} {requester.LastName}</p>
                            <p style='margin: 10px 0;'><strong>Email:</strong> {requester.Email}</p>
                            <p style='margin: 10px 0;'><strong>Topic:</strong> {topicDisplay}</p>
                            <p style='margin: 10px 0;'><strong>Languages:</strong> {languages}</p>
                        </div>
                        
                        <div style='margin: 20px 0;'>
                            <h3 style='color: #2c3e50; font-size: 16px;'>Message:</h3>
                            <div style='background-color: #f8f9fa; padding: 15px; border-left: 4px solid #3498db; border-radius: 3px;'>
                                {request.Message.Replace("\n", "<br/>")}
                            </div>
                        </div>
                        
                        <div style='margin-top: 20px; padding-top: 20px; border-top: 1px solid #ddd; color: #7f8c8d; font-size: 12px;'>
                            <p>This inquiry was submitted on {DateTime.UtcNow:f} UTC</p>
                        </div>
                    </div>
                </body>
                </html>";

            _backgroundJobClient.Enqueue(() =>
                _emailService.SendEmailAsync(CONTACT_EMAIL, subject, body, true));
        }

        return Result<Guid>.Success(inquiry.Id);
    }
}

public class CreateInquiryRequestCommandValidator : AbstractValidator<CreateInquiryRequestCommand>
{
    public CreateInquiryRequestCommandValidator()
    {
        RuleFor(x => x.Topic).IsInEnum().When(x => x.Topic.HasValue);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Languages).NotEmpty().WithMessage("At least one language must be specified.");
        RuleForEach(x => x.Languages).IsInEnum();
    }
}
