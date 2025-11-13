using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Events.Commands.CreateEvent;
using AskAMuslimBackend.Application.Events.Commands.DeleteEvent;
using AskAMuslimBackend.Application.Events.Commands.UpdateEvent;
using AskAMuslimBackend.Application.Events.Queries;
using AskAMuslimBackend.Application.Events.Queries.GetEventById;
using AskAMuslimBackend.Application.Events.Queries.GetEventsWithPagination;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AskAMuslimBackend.Web.Endpoints;

public class Events : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetEventsWithPagination).WithName("GetEvents").WithSummary("Get events");
        groupBuilder.MapGet(GetEventById, "{id}").WithName("GetEvent").WithSummary("Get event by id");
        groupBuilder.MapPost(CreateEvent).RequireAuthorization().WithName("CreateEvent").WithSummary("Create event");
        groupBuilder.MapPut(UpdateEvent, "{id}").RequireAuthorization().WithName("UpdateEvent").WithSummary("Update event");
        groupBuilder.MapDelete(DeleteEvent, "{id}").RequireAuthorization().WithName("DeleteEvent").WithSummary("Delete event");
    }

    public async Task<Ok<PaginatedList<EventDto>>> GetEventsWithPagination(ISender sender, [AsParameters] GetEventsWithPaginationQuery query)
    {
        var result = await sender.Send(query);
        return TypedResults.Ok(result);
    }

    public async Task<Results<Ok<EventDto>, NotFound>> GetEventById(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetEventByIdQuery(id));
        return result != null ? TypedResults.Ok(result) : TypedResults.NotFound();
    }

    public async Task<Created<Guid>> CreateEvent(ISender sender, CreateEventCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/{nameof(Events)}/{id}", id);
    }

    public async Task<Results<NoContent, BadRequest>> UpdateEvent(ISender sender, Guid id, UpdateEventCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();
        await sender.Send(command);
        return TypedResults.NoContent();
    }

    public async Task<NoContent> DeleteEvent(ISender sender, Guid id)
    {
        await sender.Send(new DeleteEventCommand(id));
        return TypedResults.NoContent();
    }
}
