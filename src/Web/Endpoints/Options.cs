using AskAMuslimBackend.Application.Options.Commands.CreateOption;
using AskAMuslimBackend.Application.Options.Commands.DeleteOption;
using AskAMuslimBackend.Application.Options.Commands.UpdateOption;
using AskAMuslimBackend.Application.Options.Queries;
using AskAMuslimBackend.Application.Options.Queries.GetOptionsByQuestion;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AskAMuslimBackend.Web.Endpoints;

public class Options : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetOptionsByQuestion, "by-question/{questionId}").WithName("GetOptionsByQuestion").WithSummary("Get options for a question");
        groupBuilder.MapPost(CreateOption).RequireAuthorization().WithName("CreateOption").WithSummary("Create a new option");
        groupBuilder.MapPut(UpdateOption, "{id}").RequireAuthorization().WithName("UpdateOption").WithSummary("Update an option");
        groupBuilder.MapDelete(DeleteOption, "{id}").RequireAuthorization().WithName("DeleteOption").WithSummary("Delete an option");
    }

    public async Task<Ok<List<OptionBriefDto>>> GetOptionsByQuestion(ISender sender, Guid questionId)
    {
        var result = await sender.Send(new GetOptionsByQuestionQuery(questionId));
        return TypedResults.Ok(result);
    }

    public async Task<Created<Guid>> CreateOption(ISender sender, CreateOptionCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/{nameof(Options)}/{id}", id);
    }

    public async Task<Results<NoContent, BadRequest>> UpdateOption(ISender sender, Guid id, UpdateOptionCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();
        await sender.Send(command);
        return TypedResults.NoContent();
    }

    public async Task<NoContent> DeleteOption(ISender sender, Guid id)
    {
        await sender.Send(new DeleteOptionCommand(id));
        return TypedResults.NoContent();
    }
}
