using AskAMuslimBackend.Application.LearnIslamRequests.Commands.CreateLearnIslamRequest;
using AskAMuslimBackend.Application.LearnIslamRequests.Commands.UpdateLearnIslamRequestLabel;
using AskAMuslimBackend.Application.LearnIslamRequests.Commands.UpdateLearnIslamRequestStatus;
using AskAMuslimBackend.Application.LearnIslamRequests.Queries.GetLearnIslamRequests;
using AskAMuslimBackend.Domain.Constants;
using AskAMuslimBackend.Web.Infrastructure;

namespace AskAMuslimBackend.Web.Endpoints;

public class LearnIslamRequests : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(CreateLearnIslamRequest)
            .AllowAnonymous()
            .WithName("CreateLearnIslamRequest")
            .WithSummary("Submit a new request to learn about Islam");

        groupBuilder.MapGet(GetLearnIslamRequests)
            .RequireAuthorization(Policies.ManageAdmins) // Assuming admins view these
            .WithName("GetLearnIslamRequests")
            .WithSummary("Get a list of all learn Islam requests");

        groupBuilder.MapPut(UpdateStatus, "{id}/status")
            .RequireAuthorization(Policies.ManageAdmins)
            .WithName("UpdateLearnIslamRequestStatus")
            .WithSummary("Update the status of a learn Islam request");

        groupBuilder.MapPut(UpdateLabel, "{id}/label")
            .RequireAuthorization(Policies.ManageAdmins)
            .WithName("UpdateLearnIslamRequestLabel")
            .WithSummary("Update the label of a learn Islam request");
    }

    public async Task<IResult> CreateLearnIslamRequest(ISender sender, CreateLearnIslamRequestCommand command)
    {
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    public async Task<IResult> GetLearnIslamRequests(ISender sender)
    {
        var result = await sender.Send(new GetLearnIslamRequestsQuery());
        return result.ToHttpResult();
    }

    public async Task<IResult> UpdateStatus(ISender sender, Guid id, UpdateLearnIslamRequestStatusCommand command)
    {
        var result = await sender.Send(command with { Id = id });
        return result.ToHttpResult();
    }

    public async Task<IResult> UpdateLabel(ISender sender, Guid id, UpdateLearnIslamRequestLabelCommand command)
    {
        var result = await sender.Send(command with { Id = id });
        return result.ToHttpResult();
    }
}
