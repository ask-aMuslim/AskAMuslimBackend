using AskAMuslimBackend.Application.Mosques.Commands.CreateMosque;
using AskAMuslimBackend.Application.Mosques.Commands.DeleteMosque;
using AskAMuslimBackend.Application.Mosques.Commands.ManageMosqueStatus;
using AskAMuslimBackend.Application.Mosques.Commands.UpdateMosque;
using AskAMuslimBackend.Application.Mosques.Queries;
using AskAMuslimBackend.Application.Mosques.Queries.GetMosqueById;
using AskAMuslimBackend.Application.Mosques.Queries.GetMosques;
using AskAMuslimBackend.Application.Mosques.Queries.GetNearbyMosques;
using AskAMuslimBackend.Domain.Constants;
using AskAMuslimBackend.Web.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AskAMuslimBackend.Web.Endpoints;

public class Mosques : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetMosques)
            .WithName("GetMosques")
            .WithSummary("Get mosques")
            .AllowAnonymous();

        groupBuilder.MapGet(GetMosqueById, "{id}")
            .WithName("GetMosqueById")
            .WithSummary("Get mosque by id")
            .AllowAnonymous();

        groupBuilder.MapPost(CreateMosque)
            .RequireAuthorization(Policies.ManageAdmins)
            .WithName("CreateMosque")
            .WithSummary("Create mosque");

        groupBuilder.MapPut(UpdateMosque, "{id}")
            .RequireAuthorization(Policies.ManageAdmins)
            .WithName("UpdateMosque")
            .WithSummary("Update mosque");

        groupBuilder.MapDelete(DeleteMosque, "{id}")
            .RequireAuthorization(Policies.ManageAdmins)
            .WithName("DeleteMosque")
            .WithSummary("Delete mosque");

        groupBuilder.MapGet(SearchMosques, "search")
            .WithName("SearchMosques")
            .WithSummary("Search mosques")
            .AllowAnonymous();

        groupBuilder.MapGet(GetNearbyMosques, "nearby")
            .WithName("GetNearbyMosques")
            .WithSummary("Get nearby mosques")
            .AllowAnonymous();

        groupBuilder.MapGet(GetMosquesInRadius, "in-radius")
            .WithName("GetMosquesInRadius")
            .WithSummary("Get mosques in radius")
            .AllowAnonymous();

        groupBuilder.MapGet(GetNearestMosque, "nearest")
            .WithName("GetNearestMosque")
            .WithSummary("Get nearest mosque")
            .AllowAnonymous();

        groupBuilder.MapPut(ActivateMosque, "{id}/activate")
            .RequireAuthorization(Policies.ManageAdmins)
            .WithName("ActivateMosque")
            .WithSummary("Activate mosque");

        groupBuilder.MapPut(DeactivateMosque, "{id}/deactivate")
            .RequireAuthorization(Policies.ManageAdmins)
            .WithName("DeactivateMosque")
            .WithSummary("Deactivate mosque");

        groupBuilder.MapPut(VerifyMosque, "{id}/verify")
            .RequireAuthorization(Policies.ManageAdmins)
            .WithName("VerifyMosque")
            .WithSummary("Verify mosque");
    }

    public async Task<IResult> GetMosques(ISender sender, [AsParameters] GetMosquesQuery query)
    {
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    public async Task<IResult> SearchMosques(ISender sender, [AsParameters] GetMosquesQuery query)
    {
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    public async Task<IResult> GetMosqueById(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetMosqueByIdQuery(id));
        return result.ToHttpResult();
    }

    public async Task<IResult> CreateMosque(ISender sender, CreateMosqueCommand command)
    {
        var result = await sender.Send(command);
        return result.ToCreatedResult($"/{nameof(Mosques)}/{result.Data}");
    }

    public async Task<IResult> UpdateMosque(ISender sender, Guid id, UpdateMosqueCommand command)
    {
        var result = await sender.Send(command with { Id = id });
        return result.ToHttpResult();
    }

    public async Task<IResult> DeleteMosque(ISender sender, Guid id)
    {
        var result = await sender.Send(new DeleteMosqueCommand(id));
        return result.ToHttpResult();
    }

    public async Task<IResult> GetNearbyMosques(ISender sender, [AsParameters] GetNearbyMosquesQuery query)
    {
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    public async Task<IResult> GetMosquesInRadius(ISender sender, [AsParameters] GetMosquesInRadiusQuery query)
    {
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    public async Task<IResult> GetNearestMosque(ISender sender, [AsParameters] GetNearestMosqueQuery query)
    {
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    public async Task<IResult> ActivateMosque(ISender sender, Guid id)
    {
        var result = await sender.Send(new ActivateMosqueCommand(id));
        return result.ToHttpResult();
    }

    public async Task<IResult> DeactivateMosque(ISender sender, Guid id)
    {
        var result = await sender.Send(new DeactivateMosqueCommand(id));
        return result.ToHttpResult();
    }

    public async Task<IResult> VerifyMosque(ISender sender, Guid id)
    {
        var result = await sender.Send(new VerifyMosqueCommand(id));
        return result.ToHttpResult();
    }
}
