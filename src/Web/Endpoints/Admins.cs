using AskAMuslimBackend.Application.Admins.Commands.UpdateAdminProfile;
using AskAMuslimBackend.Application.Admins.Queries;
using AskAMuslimBackend.Application.Admins.Queries.GetAdminProfile;
using AskAMuslimBackend.Web.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AskAMuslimBackend.Web.Endpoints;

public class Admins : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetMyProfile, "me")
            .RequireAuthorization()
            .WithName("GetMyAdminProfile")
            .WithSummary("Get current admin's profile");

        groupBuilder.MapGet(GetAdminProfile, "{userId}")
            .RequireAuthorization()
            .WithName("GetAdminProfile")
            .WithSummary("Get admin profile by user ID");

        groupBuilder.MapPut(UpdateAdminProfile, "me")
            .RequireAuthorization()
            .WithName("UpdateAdminProfile")
            .WithSummary("Update current admin's profile");
    }

    public async Task<IResult> GetMyProfile(ISender sender)
    {
        var result = await sender.Send(new GetAdminProfileQuery());
        return result.ToHttpResult();
    }

    public async Task<IResult> GetAdminProfile(ISender sender, string userId)
    {
        var result = await sender.Send(new GetAdminProfileQuery { UserId = userId });
        return result.ToHttpResult();
    }

    public async Task<IResult> UpdateAdminProfile(ISender sender, UpdateAdminProfileCommand command)
    {
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }
}
