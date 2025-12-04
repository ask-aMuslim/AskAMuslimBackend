using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Tags.Commands.CreateTag;
using AskAMuslimBackend.Application.Tags.Commands.DeleteTag;
using AskAMuslimBackend.Application.Tags.Commands.UpdateTag;
using AskAMuslimBackend.Application.Tags.Queries;
using AskAMuslimBackend.Application.Tags.Queries.GetTagsWithPagination;
using AskAMuslimBackend.Application.Tags.Queries.GetTagById;
using AskAMuslimBackend.Web.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AskAMuslimBackend.Web.Endpoints;

public class Tags : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetTagsWithPagination).WithName("GetTags").WithSummary("Get tags");
        groupBuilder.MapGet(GetTagById, "{id}").WithName("GetTag").WithSummary("Get a tag by id");
        groupBuilder.MapPost(CreateTag).RequireAuthorization().WithName("CreateTag").WithSummary("Create a new tag");
        groupBuilder.MapPut(UpdateTag, "{id}").RequireAuthorization().WithName("UpdateTag").WithSummary("Update a tag");
        groupBuilder.MapDelete(DeleteTag, "{id}").RequireAuthorization().WithName("DeleteTag").WithSummary("Delete a tag");
    }

    public async Task<IResult> GetTagsWithPagination(ISender sender, [AsParameters] GetTagsWithPaginationQuery query)
    {
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    public async Task<IResult> GetTagById(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetTagByIdQuery(id));
        return result.ToHttpResult();
    }

    public async Task<IResult> CreateTag(ISender sender, CreateTagCommand command)
    {
        var result = await sender.Send(command);
        return result.ToCreatedResult($"/{nameof(Tags)}/{result.Data}");
    }

    public async Task<IResult> UpdateTag(ISender sender, Guid id, UpdateTagCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    public async Task<IResult> DeleteTag(ISender sender, Guid id)
    {
        var result = await sender.Send(new DeleteTagCommand(id));
        return result.ToHttpResult();
    }
}
