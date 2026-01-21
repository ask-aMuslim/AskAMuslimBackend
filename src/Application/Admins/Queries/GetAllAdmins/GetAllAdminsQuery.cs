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
        var admins = await _context.Admins
            .Select(a => new AdminDto
            {
                Id = a.Id,
                UserId = a.UserId,
                Email = string.Empty, // Will be populated from Identity if needed
                FirstName = string.Empty,
                LastName = string.Empty,
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
