using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.QAs.Commands.CreateQA;
using AskAMuslimBackend.Application.QAs.Commands.DeleteQA;
using AskAMuslimBackend.Application.QAs.Commands.UpdateQA;
using AskAMuslimBackend.Application.QAs.Queries;
using AskAMuslimBackend.Application.QAs.Queries.GetQAById;
using AskAMuslimBackend.Application.QAs.Queries.GetQAsWithPagination;
using AskAMuslimBackend.Web.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AskAMuslimBackend.Web.Endpoints;

public class QAs : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetQAsWithPagination).WithName("GetQAs").WithSummary("Get QAs");
        groupBuilder.MapGet(GetQAById, "{id}").WithName("GetQA").WithSummary("Get QA by id");
        groupBuilder.MapPost(CreateQA).RequireAuthorization().WithName("CreateQA").WithSummary("Create QA");
        groupBuilder.MapPut(UpdateQA, "{id}").RequireAuthorization().WithName("UpdateQA").WithSummary("Update QA");
        groupBuilder.MapDelete(DeleteQA, "{id}").RequireAuthorization().WithName("DeleteQA").WithSummary("Delete QA");
    }

    public async Task<IResult> GetQAsWithPagination(ISender sender, [AsParameters] GetQAsWithPaginationQuery query)
    {
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    public async Task<IResult> GetQAById(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetQAByIdQuery(id));
        return result.ToHttpResult();
    }

    public async Task<IResult> CreateQA(ISender sender, CreateQACommand command)
    {
        var result = await sender.Send(command);
        return result.ToCreatedResult($"/{nameof(QAs)}/{result.Data}");
    }

    public async Task<IResult> UpdateQA(ISender sender, Guid id, UpdateQACommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    public async Task<IResult> DeleteQA(ISender sender, Guid id)
    {
        var result = await sender.Send(new DeleteQACommand(id));
        return result.ToHttpResult();
    }
}
