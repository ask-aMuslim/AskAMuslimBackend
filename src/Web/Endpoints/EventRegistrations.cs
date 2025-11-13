using AskAMuslimBackend.Application.EventRegistrations.Commands.CreateEventRegistration;
using AskAMuslimBackend.Application.EventRegistrations.Commands.DeleteEventRegistration;
using AskAMuslimBackend.Application.EventRegistrations.Commands.UpdateEventRegistrationStatus;
using AskAMuslimBackend.Application.EventRegistrations.Queries;
using AskAMuslimBackend.Application.EventRegistrations.Queries.GetEventRegistrationById;
using AskAMuslimBackend.Application.EventRegistrations.Queries.GetEventRegistrationsByEvent;
using AskAMuslimBackend.Application.EventRegistrations.Queries.GetEventRegistrationsByUser;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AskAMuslimBackend.Web.Endpoints;

public class EventRegistrations : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetRegistration, "{id}").RequireAuthorization().WithName("GetEventRegistration").WithSummary("Get an event registration by id");
        groupBuilder.MapGet(GetByEvent, "by-event/{eventId}").RequireAuthorization().WithName("GetEventRegistrationsByEvent").WithSummary("Get registrations for an event");
        groupBuilder.MapGet(GetByUser, "by-user/{userId}").RequireAuthorization().WithName("GetEventRegistrationsByUser").WithSummary("Get registrations for a user");
        groupBuilder.MapPost(CreateRegistration).RequireAuthorization().WithName("CreateEventRegistration").WithSummary("Create a new event registration");
        groupBuilder.MapPut(UpdateRegistrationStatus, "{id}/status").RequireAuthorization().WithName("UpdateEventRegistrationStatus").WithSummary("Update event registration status");
        groupBuilder.MapDelete(DeleteRegistration, "{id}").RequireAuthorization().WithName("DeleteEventRegistration").WithSummary("Delete an event registration");
    }

    public async Task<Results<Ok<EventRegistrationDto>, NotFound>> GetRegistration(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetEventRegistrationByIdQuery(id));
        return result != null ? TypedResults.Ok(result) : TypedResults.NotFound();
    }

    public async Task<Ok<List<EventRegistrationDto>>> GetByEvent(ISender sender, Guid eventId)
    {
        var result = await sender.Send(new GetEventRegistrationsByEventQuery(eventId));
        return TypedResults.Ok(result);
    }

    public async Task<Ok<List<EventRegistrationDto>>> GetByUser(ISender sender, Guid userId)
    {
        var result = await sender.Send(new GetEventRegistrationsByUserQuery(userId));
        return TypedResults.Ok(result);
    }

    public async Task<Created<Guid>> CreateRegistration(ISender sender, CreateEventRegistrationCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/{nameof(EventRegistrations)}/{id}", id);
    }

    public async Task<Results<NoContent, BadRequest>> UpdateRegistrationStatus(ISender sender, Guid id, UpdateEventRegistrationStatusCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();
        await sender.Send(command);
        return TypedResults.NoContent();
    }

    public async Task<NoContent> DeleteRegistration(ISender sender, Guid id)
    {
        await sender.Send(new DeleteEventRegistrationCommand(id));
        return TypedResults.NoContent();
    }
}
