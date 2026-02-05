using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Mappings;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Admins.Queries.GetAllAdmins;

public record GetAllAdminsQuery : IRequest<Result<PaginatedList<AdminDto>>>, IPaginationQuery
{
    public int? PageNumber { get; init; } = 1;
    public int? PageSize { get; init; } = 10;
}

public class GetAllAdminsQueryHandler : IRequestHandler<GetAllAdminsQuery, Result<PaginatedList<AdminDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetAllAdminsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<AdminDto>>> Handle(GetAllAdminsQuery request, CancellationToken cancellationToken)
    {
        var query = from a in _context.Admins
                    join u in _context.Users on a.UserId equals u.Id into users
                    from u in users.DefaultIfEmpty()
                    select new AdminDto
                    {
                        Id = a.Id,
                        UserId = a.UserId,
                        Email = u != null ? u.Email ?? string.Empty : string.Empty,
                        FirstName = u != null ? u.FirstName ?? string.Empty : string.Empty,
                        LastName = u != null ? u.LastName ?? string.Empty : string.Empty,
                        Bio = a.Bio,
                        ImageUrl = u != null ? u.ImageUrl ?? a.ImageUrl : a.ImageUrl,
                        Department = a.Department,
                        PhoneNumber = a.PhoneNumber,
                        IsSuperAdmin = a.IsSuperAdmin,
                        IsActive = a.IsActive,
                        JoinedDate = a.JoinedDate,
                        LastLoginDate = a.LastLoginDate
                    };

        var paginatedList = await query
            .OrderBy(a => a.LastName)
            .ThenBy(a => a.FirstName)
            .PaginatedListAsync(request.PageNumber ?? 1, request.PageSize ?? 10, cancellationToken);

        return Result<PaginatedList<AdminDto>>.Success(paginatedList);
    }
}
