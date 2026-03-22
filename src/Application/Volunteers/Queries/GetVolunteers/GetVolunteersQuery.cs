using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Volunteers.Queries.GetVolunteers;

public record GetVolunteersQuery : IRequest<Result<List<VolunteerDto>>>;

public class GetVolunteersQueryHandler : IRequestHandler<GetVolunteersQuery, Result<List<VolunteerDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetVolunteersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<VolunteerDto>>> Handle(GetVolunteersQuery request, CancellationToken cancellationToken)
    {
        var volunteers = await _context.Volunteers
            .OrderByDescending(v => v.Created)
            .Select(v => new VolunteerDto
            {
                Id = v.Id,
                Name = v.Name,
                Email = v.Email,
                Phone = v.Phone,
                Type = v.Type,
                Message = v.Message,
                Created = v.Created.DateTime // Assuming Created is DateTimeOffset
            })
            .ToListAsync(cancellationToken);

        return Result<List<VolunteerDto>>.Success(volunteers);
    }
}