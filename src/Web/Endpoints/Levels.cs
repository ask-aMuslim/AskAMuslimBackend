using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Levels.Commands.CreateLevel;
using AskAMuslimBackend.Application.Levels.Commands.DeleteLevel;
using AskAMuslimBackend.Application.Levels.Commands.UpdateLevel;
using AskAMuslimBackend.Application.Levels.Queries.GetLevelsWithPagination;
using AskAMuslimBackend.Application.Levels.Queries.GetLevelById;
using AskAMuslimBackend.Application.Levels.Queries;
using Microsoft.AspNetCore.Http.HttpResults;
using AskAMuslimBackend.Domain.Constants;

namespace AskAMuslimBackend.Web.Endpoints;

public class Levels : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetLevelsWithPagination).WithName("GetLevels").WithSummary("Get levels lookups and lists");
        groupBuilder.MapGet(GetLevelById, "{id}").WithName("GetLevel").WithSummary("Get a level by id");
        groupBuilder.MapPost(CreateLevel)
            .RequireAuthorization(Policies.ManageLevels)
            .WithName("CreateLevel")
            .WithSummary("Create a new level");
        groupBuilder.MapPut(UpdateLevel, "{id}")
            .RequireAuthorization(Policies.ManageLevels)
            .WithName("UpdateLevel")
            .WithSummary("Update a level");
        groupBuilder.MapDelete(DeleteLevel, "{id}")
            .RequireAuthorization(Policies.ManageLevels)
            .WithName("DeleteLevel")
            .WithSummary("Delete a level");
    }

    public async Task<IResult> GetLevelsWithPagination(ISender sender, [AsParameters] GetLevelsWithPaginationQuery query)
    {
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    public async Task<IResult> GetLevelById(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetLevelByIdQuery(id));
        return result.ToHttpResult();
    }

    public async Task<IResult> CreateLevel(ISender sender, CreateLevelCommand command)
    {
        var result = await sender.Send(command);
        return result.ToCreatedResult($"/{nameof(Levels)}/{result.Data}");
    }

    public async Task<IResult> UpdateLevel(ISender sender, Guid id, UpdateLevelCommand command)
    {
        var result = await sender.Send(command with { Id = id });
        return result.ToHttpResult();
    }

    public async Task<IResult> DeleteLevel(ISender sender, Guid id)
    {
        var result = await sender.Send(new DeleteLevelCommand(id));
        return result.ToHttpResult();
    }
}
