using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Admins.Queries.GetAllAdmins;

public record GetAllAdminsQuery : IRequest<Result<List<AdminDto>>>;

public class GetAllAdminsQueryHandler : IRequestHandler<GetAllAdminsQuery, Result<List<AdminDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetAllAdminsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<AdminDto>>> Handle(GetAllAdminsQuery request, CancellationToken cancellationToken)
    {
        var admins = await (from a in _context.Admins
                            join u in _context.Users on a.UserId equals u.Id into users
                            from u in users.DefaultIfEmpty()
                            select new AdminDto
                            {
                                Id = a.Id,
                                UserId = a.UserId,
                                Email = u != null ? u.Email ?? string.Empty : string.Empty,
                                FirstName = u != null ? u.FirstName ?? string.Empty : string.Empty,
                                LastName = u != null ? u.LastName ?? string.Empty : string.Empty,
                                Department = a.Department,
                                PhoneNumber = a.PhoneNumber,
                                IsSuperAdmin = a.IsSuperAdmin,
                                IsActive = a.IsActive,
                                JoinedDate = a.JoinedDate,
                                LastLoginDate = a.LastLoginDate
                            })
                            .ToListAsync(cancellationToken);

        return Result<List<AdminDto>>.Success(admins);
    }
}
