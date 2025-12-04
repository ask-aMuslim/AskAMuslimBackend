using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.Preachers.Commands.UpdatePreacherProfile;

public record UpdatePreacherProfileCommand : IRequest<Result<Guid>>
{
    public string? Bio { get; init; }
    public string? Specialization { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Website { get; init; }
    public string? LinkedInProfile { get; init; }
    public int? YearsOfExperience { get; init; }
    public string? Certifications { get; init; }
    public string? Organization { get; init; }
}

public class UpdatePreacherProfileCommandHandler : IRequestHandler<UpdatePreacherProfileCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public UpdatePreacherProfileCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(UpdatePreacherProfileCommand request, CancellationToken cancellationToken)
    {
        var userIdString = _currentUser.Id;
        
        if (string.IsNullOrEmpty(userIdString))
        {
            return Result<Guid>.Failure(new[] { "User not authenticated" });
        }

        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result<Guid>.Failure(new[] { "Invalid user ID format" });
        }

        var preacher = await _context.Preachers
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
            
        if (preacher == null)
        {
            return Result<Guid>.Failure(new[] { $"Preacher with user ID {userId} not found" });
        }

        if (request.Bio != null) preacher.Bio = request.Bio;
        if (request.Specialization != null) preacher.Specialization = request.Specialization;
        if (request.PhoneNumber != null) preacher.PhoneNumber = request.PhoneNumber;
        if (request.Website != null) preacher.Website = request.Website;
        if (request.LinkedInProfile != null) preacher.LinkedInProfile = request.LinkedInProfile;
        if (request.YearsOfExperience.HasValue) preacher.YearsOfExperience = request.YearsOfExperience.Value;
        if (request.Certifications != null) preacher.Certifications = request.Certifications;
        if (request.Organization != null) preacher.Organization = request.Organization;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(preacher.Id);
    }
}
