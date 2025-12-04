using AskAMuslimBackend.Application.Preachers.Commands.UpdatePreacherProfile;
using AskAMuslimBackend.Application.Preachers.Queries;
using AskAMuslimBackend.Application.Preachers.Queries.GetPreacherProfile;
using AskAMuslimBackend.Application.Preachers.Queries.GetAllPreachers;
using AskAMuslimBackend.Web.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AskAMuslimBackend.Web.Endpoints;

public class Preachers : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetMyProfile, "me")
            .RequireAuthorization()
            .WithName("GetMyPreacherProfile")
            .WithSummary("Get current preacher's profile");

        groupBuilder.MapGet(GetPreacherProfile, "{userId}")
            .RequireAuthorization()
            .WithName("GetPreacherProfile")
            .WithSummary("Get preacher profile by user ID");

        groupBuilder.MapGet(GetAllPreachers)
            .WithName("GetAllPreachers")
            .WithSummary("Get all active preachers");

        groupBuilder.MapPut(UpdatePreacherProfile, "me")
            .RequireAuthorization()
            .WithName("UpdatePreacherProfile")
            .WithSummary("Update current preacher's profile");
    }

    public async Task<IResult> GetMyProfile(ISender sender)
    {
        var result = await sender.Send(new GetPreacherProfileQuery());
        return result.ToHttpResult();
    }

    public async Task<IResult> GetPreacherProfile(ISender sender, string userId)
    {
        var result = await sender.Send(new GetPreacherProfileQuery { UserId = userId });
        return result.ToHttpResult();
    }

    public async Task<IResult> GetAllPreachers(ISender sender)
    {
        var result = await sender.Send(new GetAllPreachersQuery());
        return result.ToHttpResult();
    }

    public async Task<IResult> UpdatePreacherProfile(ISender sender, UpdatePreacherProfileCommand command)
    {
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }
}
