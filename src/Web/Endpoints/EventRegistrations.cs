using AskAMuslimBackend.Application.EventRegistrations.Commands.CreateEventRegistration;
using AskAMuslimBackend.Application.EventRegistrations.Commands.DeleteEventRegistration;
using AskAMuslimBackend.Application.EventRegistrations.Commands.UpdateEventRegistrationStatus;
using AskAMuslimBackend.Application.EventRegistrations.Queries;
using AskAMuslimBackend.Application.EventRegistrations.Queries.GetEventRegistrationById;
using AskAMuslimBackend.Application.EventRegistrations.Queries.GetEventRegistrationsByEvent;
using AskAMuslimBackend.Application.EventRegistrations.Queries.GetEventRegistrationsByUser;
using AskAMuslimBackend.Domain.Constants;
using AskAMuslimBackend.Web.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AskAMuslimBackend.Web.Endpoints;

public class EventRegistrations : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetRegistration, "{id}")
            .RequireAuthorization(Policies.ViewEvents)
            .WithName("GetEventRegistration")
            .WithSummary("Get an event registration by id");
        groupBuilder.MapGet(GetByEvent, "by-event/{eventId}")
            .RequireAuthorization(Policies.ViewEvents)
            .WithName("GetEventRegistrationsByEvent")
            .WithSummary("Get registrations for an event");
        groupBuilder.MapGet(GetByUser, "by-user/{userId}")
            .RequireAuthorization(Policies.ViewEvents)
            .WithName("GetEventRegistrationsByUser")
            .WithSummary("Get registrations for a user");
        groupBuilder.MapPost(CreateRegistration)
            .RequireAuthorization()
            .WithName("CreateEventRegistration")
            .WithSummary("Create a new event registration");
        groupBuilder.MapPut(UpdateRegistrationStatus, "{id}/status")
            .RequireAuthorization(Policies.ManageEventRegistrations)
            .WithName("UpdateEventRegistrationStatus")
            .WithSummary("Update event registration status");
        groupBuilder.MapDelete(DeleteRegistration, "{id}")
            .RequireAuthorization(Policies.ManageEventRegistrations)
            .WithName("DeleteEventRegistration")
            .WithSummary("Delete an event registration");
    }

    public async Task<IResult> GetRegistration(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetEventRegistrationByIdQuery(id));
        return result.ToHttpResult();
    }

    public async Task<IResult> GetByEvent(ISender sender, Guid eventId)
    {
        var result = await sender.Send(new GetEventRegistrationsByEventQuery(eventId));
        return result.ToHttpResult();
    }

    public async Task<IResult> GetByUser(ISender sender, Guid userId)
    {
        var result = await sender.Send(new GetEventRegistrationsByUserQuery(userId));
        return result.ToHttpResult();
    }

    public async Task<IResult> CreateRegistration(ISender sender, CreateEventRegistrationCommand command)
    {
        var result = await sender.Send(command);
        return result.ToCreatedResult($"/{nameof(EventRegistrations)}/{result.Data}");
    }

    public async Task<IResult> UpdateRegistrationStatus(ISender sender, Guid id, UpdateEventRegistrationStatusCommand command)
    {
        var result = await sender.Send(command with { Id = id });
        return result.ToHttpResult();
    }

    public async Task<IResult> DeleteRegistration(ISender sender, Guid id)
    {
        var result = await sender.Send(new DeleteEventRegistrationCommand(id));
        return result.ToHttpResult();
    }
}
