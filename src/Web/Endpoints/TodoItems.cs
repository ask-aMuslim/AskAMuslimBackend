using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.TodoItems.Commands.CreateTodoItem;
using AskAMuslimBackend.Application.TodoItems.Commands.DeleteTodoItem;
using AskAMuslimBackend.Application.TodoItems.Commands.UpdateTodoItem;
using AskAMuslimBackend.Application.TodoItems.Commands.UpdateTodoItemDetail;
using AskAMuslimBackend.Application.TodoItems.Queries.GetTodoItemsWithPagination;
using AskAMuslimBackend.Web.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AskAMuslimBackend.Web.Endpoints;

public class TodoItems : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetTodoItemsWithPagination).RequireAuthorization();
        groupBuilder.MapPost(CreateTodoItem).RequireAuthorization();
        groupBuilder.MapPut(UpdateTodoItem, "{id}").RequireAuthorization();
        groupBuilder.MapPut(UpdateTodoItemDetail, "UpdateDetail/{id}").RequireAuthorization();
        groupBuilder.MapDelete(DeleteTodoItem, "{id}").RequireAuthorization();
    }

    public async Task<IResult> GetTodoItemsWithPagination(ISender sender, [AsParameters] GetTodoItemsWithPaginationQuery query)
    {
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    public async Task<IResult> CreateTodoItem(ISender sender, CreateTodoItemCommand command)
    {
        var result = await sender.Send(command);
        return result.ToCreatedResult($"/{nameof(TodoItems)}/{result.Data}");
    }

    public async Task<IResult> UpdateTodoItem(ISender sender, Guid id, UpdateTodoItemCommand command)
    {
        if (id != Guid.Empty && id != new Guid(command.Id.ToString())) return TypedResults.BadRequest();
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    public async Task<IResult> UpdateTodoItemDetail(ISender sender, Guid id, UpdateTodoItemDetailCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    public async Task<IResult> DeleteTodoItem(ISender sender, Guid id)
    {
        var result = await sender.Send(new DeleteTodoItemCommand(id));
        return result.ToHttpResult();
    }
}
