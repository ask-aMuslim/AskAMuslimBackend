using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.Preachers.Queries.GetAllPreachers;

public record GetAllPreachersQuery : IRequest<List<PreacherDto>>;

public class GetAllPreachersQueryHandler : IRequestHandler<GetAllPreachersQuery, List<PreacherDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAllPreachersQueryHandler(
        IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PreacherDto>> Handle(GetAllPreachersQuery request, CancellationToken cancellationToken)
    {
        var preachers = await _context.Preachers
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);

        var result = new List<PreacherDto>();

        foreach (var preacher in preachers)
        {
            var user = await _context.Users.FindAsync(new object[] { preacher.UserId }, cancellationToken);
            
            result.Add(new PreacherDto
            {
                Id = preacher.Id,
                UserId = preacher.UserId,
                Email = user?.Email ?? string.Empty,
                FirstName = user?.FirstName ?? string.Empty,
                LastName = user?.LastName ?? string.Empty,
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

        return result;
    }
}
