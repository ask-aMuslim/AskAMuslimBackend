using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Newsletters.Commands.SubscribeToNewsletter;

public record SubscribeToNewsletterCommand : IRequest<Result<Guid>>
{
    public string Email { get; init; } = string.Empty;
}

public class SubscribeToNewsletterCommandValidator : AbstractValidator<SubscribeToNewsletterCommand>
{
    public SubscribeToNewsletterCommandValidator()
    {
        RuleFor(v => v.Email)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("A valid email address is required.");
    }
}

public class SubscribeToNewsletterCommandHandler : IRequestHandler<SubscribeToNewsletterCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public SubscribeToNewsletterCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(SubscribeToNewsletterCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.ToLowerInvariant();

        var existingSubscription = await _context.NewsletterSubscriptions
            .FirstOrDefaultAsync(n => n.Email == normalizedEmail, cancellationToken);

        if (existingSubscription != null)
        {
            if (!existingSubscription.IsActive)
            {
                existingSubscription.IsActive = true;
                existingSubscription.UnsubscribedAt = null;
                await _context.SaveChangesAsync(cancellationToken);
            }
            
            return Result<Guid>.Success(existingSubscription.Id);
        }

        var subscription = new NewsletterSubscription
        {
            Email = normalizedEmail,
            IsActive = true
        };

        _context.NewsletterSubscriptions.Add(subscription);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(subscription.Id);
    }
}