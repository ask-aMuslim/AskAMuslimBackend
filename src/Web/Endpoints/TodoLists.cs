using AskAMuslimBackend.Application.TodoLists.Commands.CreateTodoList;
using AskAMuslimBackend.Application.TodoLists.Commands.DeleteTodoList;
using AskAMuslimBackend.Application.TodoLists.Commands.UpdateTodoList;
using AskAMuslimBackend.Application.TodoLists.Queries.GetTodos;
using AskAMuslimBackend.Web.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AskAMuslimBackend.Web.Endpoints;

public class TodoLists : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetTodoLists).RequireAuthorization();
        groupBuilder.MapPost(CreateTodoList).RequireAuthorization();
        groupBuilder.MapPut(UpdateTodoList, "{id}").RequireAuthorization();
        groupBuilder.MapDelete(DeleteTodoList, "{id}").RequireAuthorization();
    }

    public async Task<IResult> GetTodoLists(ISender sender)
    {
        var result = await sender.Send(new GetTodosQuery());
        return result.ToHttpResult();
    }

    public async Task<IResult> CreateTodoList(ISender sender, CreateTodoListCommand command)
    {
        var result = await sender.Send(command);
        return result.ToCreatedResult($"/{nameof(TodoLists)}/{result.Data}");
    }

    public async Task<IResult> UpdateTodoList(ISender sender, Guid id, UpdateTodoListCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    public async Task<IResult> DeleteTodoList(ISender sender, Guid id)
    {
        var result = await sender.Send(new DeleteTodoListCommand(id));
        return result.ToHttpResult();
    }
}
