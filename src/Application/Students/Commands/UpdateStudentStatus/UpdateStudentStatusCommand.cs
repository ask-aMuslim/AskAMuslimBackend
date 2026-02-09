using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Security;
using AskAMuslimBackend.Domain.Constants;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using System.Text.Json.Serialization;

namespace AskAMuslimBackend.Application.Students.Commands.UpdateStudentStatus;

[Authorize(Policy = Policies.ManageStudents)]
public record UpdateStudentStatusCommand : IRequest<Result<bool>>
{
    [JsonIgnore] // Set from route parameter, not request body
    public Guid Id { get; init; }
    public bool IsActive { get; init; }
}

public class UpdateStudentStatusCommandValidator : AbstractValidator<UpdateStudentStatusCommand>
{
    public UpdateStudentStatusCommandValidator()
    {
    }
}

public class UpdateStudentStatusCommandHandler : IRequestHandler<UpdateStudentStatusCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<Domain.Entities.User> _userManager;

    public UpdateStudentStatusCommandHandler(
        IApplicationDbContext context,
        UserManager<Domain.Entities.User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<Result<bool>> Handle(UpdateStudentStatusCommand request, CancellationToken cancellationToken)
    {
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (student == null)
        {
            return Result<bool>.Failure(new[] { "Student not found" });
        }

        // Update student status
        student.IsActive = request.IsActive;

        // Get the associated user
        var user = await _userManager.FindByIdAsync(student.UserId.ToString());
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

        // Save student changes
        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
