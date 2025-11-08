using AskAMuslimBackend.Application.Admins.Commands.UpdateAdminProfile;
using AskAMuslimBackend.Application.Admins.Queries;
using AskAMuslimBackend.Application.Admins.Queries.GetAdminProfile;
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

    public async Task<Results<Ok<AdminDto>, NotFound>> GetMyProfile(ISender sender)
    {
        var result = await sender.Send(new GetAdminProfileQuery());
        
        return result != null ? TypedResults.Ok(result) : TypedResults.NotFound();
    }

    public async Task<Results<Ok<AdminDto>, NotFound>> GetAdminProfile(ISender sender, string userId)
    {
        var result = await sender.Send(new GetAdminProfileQuery { UserId = userId });
        
        return result != null ? TypedResults.Ok(result) : TypedResults.NotFound();
    }

    public async Task<Ok<Guid>> UpdateAdminProfile(ISender sender, UpdateAdminProfileCommand command)
    {
        var id = await sender.Send(command);
        
        return TypedResults.Ok(id);
    }
}
