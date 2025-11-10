using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Tags.Commands.CreateTag;
using AskAMuslimBackend.Application.Tags.Commands.DeleteTag;
using AskAMuslimBackend.Application.Tags.Commands.UpdateTag;
using AskAMuslimBackend.Application.Tags.Queries;
using AskAMuslimBackend.Application.Tags.Queries.GetTagsWithPagination;
using AskAMuslimBackend.Application.Tags.Queries.GetTagById;
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

    public async Task<Ok<PaginatedList<TagBriefDto>>> GetTagsWithPagination(ISender sender, [AsParameters] GetTagsWithPaginationQuery query)
    {
        var result = await sender.Send(query);
        return TypedResults.Ok(result);
    }

    public async Task<Results<Ok<TagBriefDto>, NotFound>> GetTagById(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetTagByIdQuery(id));
        return result != null ? TypedResults.Ok(result) : TypedResults.NotFound();
    }

    public async Task<Created<Guid>> CreateTag(ISender sender, CreateTagCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/{nameof(Tags)}/{id}", id);
    }

    public async Task<Results<NoContent, BadRequest>> UpdateTag(ISender sender, Guid id, UpdateTagCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();
        await sender.Send(command);
        return TypedResults.NoContent();
    }

    public async Task<NoContent> DeleteTag(ISender sender, Guid id)
    {
        await sender.Send(new DeleteTagCommand(id));
        return TypedResults.NoContent();
    }
}
