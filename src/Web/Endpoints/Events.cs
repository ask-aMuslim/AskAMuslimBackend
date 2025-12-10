using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Events.Commands.CreateEvent;
using AskAMuslimBackend.Application.Events.Commands.DeleteEvent;
using AskAMuslimBackend.Application.Events.Commands.UpdateEvent;
using AskAMuslimBackend.Application.Events.Queries;
using AskAMuslimBackend.Application.Events.Queries.GetEventById;
using AskAMuslimBackend.Application.Events.Queries.GetEventsWithPagination;
using AskAMuslimBackend.Web.Infrastructure;
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

    public async Task<IResult> GetEventsWithPagination(ISender sender, [AsParameters] GetEventsWithPaginationQuery query)
    {
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    public async Task<IResult> GetEventById(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetEventByIdQuery(id));
        return result.ToHttpResult();
    }

    public async Task<IResult> CreateEvent(ISender sender, CreateEventCommand command)
    {
        var result = await sender.Send(command);
        return result.ToCreatedResult($"/{nameof(Events)}/{result.Data}");
    }

    public async Task<IResult> UpdateEvent(ISender sender, Guid id, UpdateEventCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    public async Task<IResult> DeleteEvent(ISender sender, Guid id)
    {
        var result = await sender.Send(new DeleteEventCommand(id));
        return result.ToHttpResult();
    }
}
