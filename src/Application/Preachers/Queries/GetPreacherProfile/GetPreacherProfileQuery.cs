using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.Preachers.Queries.GetPreacherProfile;

public record GetPreacherProfileQuery : IRequest<Result<PreacherDto?>>
{
    public string? UserId { get; init; }
}

public class GetPreacherProfileQueryHandler : IRequestHandler<GetPreacherProfileQuery, Result<PreacherDto?>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public GetPreacherProfileQueryHandler(
        IApplicationDbContext context,
        IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<PreacherDto?>> Handle(GetPreacherProfileQuery request, CancellationToken cancellationToken)
    {
        var userIdString = request.UserId ?? _currentUser.Id;
        
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Result<PreacherDto?>.Failure(new[] { "Invalid user ID" });
        }

        var preacher = await _context.Preachers
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (preacher == null)
        {
            return Result<PreacherDto?>.Failure(new[] { "Preacher profile not found" });
        }

        // Get user details from Users table
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        var email = user?.Email ?? string.Empty;
        var firstName = user?.FirstName ?? string.Empty;
        var lastName = user?.LastName ?? string.Empty;

        return Result<PreacherDto?>.Success(new PreacherDto
        {
            Id = preacher.Id,
            UserId = preacher.UserId,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Bio = preacher.Bio,
            Specialization = preacher.Specialization,
            PhoneNumber = preacher.PhoneNumber,
            Website = preacher.Website,
            LinkedInProfile = preacher.LinkedInProfile,
            YearsOfExperience = preacher.YearsOfExperience,
            IsVerified = preacher.IsVerified,
            IsActive = preacher.IsActive,
            JoinedDate = preacher.JoinedDate,
            Rating = preacher.Rating,
            TotalReviews = preacher.TotalReviews,
            Certifications = preacher.Certifications,
            Organization = preacher.Organization
        });
    }
}
