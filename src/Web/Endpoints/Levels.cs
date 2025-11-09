using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Levels.Commands.CreateLevel;
using AskAMuslimBackend.Application.Levels.Commands.DeleteLevel;
using AskAMuslimBackend.Application.Levels.Commands.UpdateLevel;
using AskAMuslimBackend.Application.Levels.Queries.GetLevelsWithPagination;
using AskAMuslimBackend.Application.Levels.Queries.GetLevelById;
using AskAMuslimBackend.Application.Levels.Queries;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AskAMuslimBackend.Web.Endpoints;

public class Levels : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetLevelsWithPagination).WithName("GetLevels").WithSummary("Get levels lookups and lists");
        groupBuilder.MapGet(GetLevelById, "{id}").WithName("GetLevel").WithSummary("Get a level by id");
        groupBuilder.MapPost(CreateLevel).RequireAuthorization().WithName("CreateLevel").WithSummary("Create a new level");
        groupBuilder.MapPut(UpdateLevel, "{id}").RequireAuthorization().WithName("UpdateLevel").WithSummary("Update a level");
        groupBuilder.MapDelete(DeleteLevel, "{id}").RequireAuthorization().WithName("DeleteLevel").WithSummary("Delete a level");
    }

    public async Task<Ok<PaginatedList<LevelBriefDto>>> GetLevelsWithPagination(ISender sender, [AsParameters] GetLevelsWithPaginationQuery query)
    {
        var result = await sender.Send(query);
        return TypedResults.Ok(result);
    }

    public async Task<Results<Ok<LevelBriefDto>, NotFound>> GetLevelById(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetLevelByIdQuery(id));
        return result != null ? TypedResults.Ok(result) : TypedResults.NotFound();
    }

    public async Task<Created<Guid>> CreateLevel(ISender sender, CreateLevelCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/{nameof(Levels)}/{id}", id);
    }

    public async Task<Results<NoContent, BadRequest>> UpdateLevel(ISender sender, Guid id, UpdateLevelCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();
        await sender.Send(command);
        return TypedResults.NoContent();
    }

    public async Task<NoContent> DeleteLevel(ISender sender, Guid id)
    {
        await sender.Send(new DeleteLevelCommand(id));
        return TypedResults.NoContent();
    }
}
