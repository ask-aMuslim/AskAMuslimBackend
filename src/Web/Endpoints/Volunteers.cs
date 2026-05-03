using AskAMuslimBackend.Application.Volunteers.Commands.CreateVolunteer;
using AskAMuslimBackend.Application.Volunteers.Queries.GetVolunteers;
using AskAMuslimBackend.Domain.Constants;
using AskAMuslimBackend.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace AskAMuslimBackend.Web.Endpoints;

public class Volunteers : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(CreateVolunteer)
            .AllowAnonymous()
            .WithName("CreateVolunteer")
            .WithSummary("Submit a new volunteer application")
            .DisableAntiforgery();

        groupBuilder.MapGet(GetVolunteers)
            .RequireAuthorization(Policies.ManageAdmins) // Or whichever policy makes sense for admins to view volunteers
            .WithName("GetVolunteers")
            .WithSummary("Get a list of all volunteer applications");
    }

    public async Task<IResult> CreateVolunteer(ISender sender, [FromForm] CreateVolunteerCommand command)
    {
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    public async Task<IResult> GetVolunteers(ISender sender)
    {
        var result = await sender.Send(new GetVolunteersQuery());
        return result.ToHttpResult();
    }
}
