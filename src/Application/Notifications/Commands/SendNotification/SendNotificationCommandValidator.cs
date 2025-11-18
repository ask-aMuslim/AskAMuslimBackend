namespace AskAMuslimBackend.Application.Notifications.Commands.SendNotification;

public class SendNotificationCommandValidator : AbstractValidator<SendNotificationCommand>
{
    public SendNotificationCommandValidator()
    {
        RuleFor(v => v.UserId)
            .NotEmpty();

        RuleFor(v => v.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(v => v.Message)
            .NotEmpty()
            .MaximumLength(1000);
    }
}
