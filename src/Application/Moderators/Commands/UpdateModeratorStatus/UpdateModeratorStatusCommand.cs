using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Security;
using AskAMuslimBackend.Domain.Constants;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace AskAMuslimBackend.Application.Moderators.Commands.UpdateModeratorStatus;

[Authorize(Policy = Policies.ManageAdmins)]
public record UpdateModeratorStatusCommand : IRequest<Result<bool>>
{
    public Guid Id { get; init; }
    public bool IsActive { get; init; }
}

public class UpdateModeratorStatusCommandValidator : AbstractValidator<UpdateModeratorStatusCommand>
{
    public UpdateModeratorStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Moderator ID is required");
    }
}

public class UpdateModeratorStatusCommandHandler : IRequestHandler<UpdateModeratorStatusCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<Domain.Entities.User> _userManager;

    public UpdateModeratorStatusCommandHandler(
        IApplicationDbContext context,
        UserManager<Domain.Entities.User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<Result<bool>> Handle(UpdateModeratorStatusCommand request, CancellationToken cancellationToken)
    {
        Guid userId;
        bool found = false;

        // Try to find in Admins
        var admin = await _context.Admins
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (admin != null)
        {
            admin.IsActive = request.IsActive;
            userId = admin.UserId;
            found = true;
        }
        else
        {
            // Try to find in Instructors
            var instructor = await _context.Instructors
                .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

            if (instructor != null)
            {
                instructor.IsActive = request.IsActive;
                userId = instructor.UserId;
                found = true;
            }
            else
            {
                // Try to find in Preachers
                var preacher = await _context.Preachers
                    .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

                if (preacher != null)
                {
                    preacher.IsActive = request.IsActive;
                    userId = preacher.UserId;
                    found = true;
                }
                else
                {
                    return Result<bool>.Failure(new[] { "Moderator not found" });
                }
            }
        }

        if (!found)
        {
            return Result<bool>.Failure(new[] { "Moderator not found" });
        }

        // Get the associated user
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Result<bool>.Failure(new[] { "Associated user not found" });
        }

        // Update user status
        user.IsActive = request.IsActive;

        // If deactivating, clear refresh token to force logout
        if (!request.IsActive)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
        }

        // Save user changes
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return Result<bool>.Failure(updateResult.Errors.Select(e => e.Description).ToArray());
        }

        // Save moderator entity changes
        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
