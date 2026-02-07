using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.UserRoles.Queries.GetRoles;

public record GetRolesQuery : IRequest<Result<List<RoleDto>>>;

public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, Result<List<RoleDto>>>
{
    public Task<Result<List<RoleDto>>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = Enum.GetValues<UserRole>()
            .Select(role => new RoleDto
            {
                Id = (int)role,
                Name = role.ToString()
            })
            .ToList();

        return Task.FromResult(Result<List<RoleDto>>.Success(roles));
    }
}
