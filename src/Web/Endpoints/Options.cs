using AskAMuslimBackend.Application.Options.Commands.CreateOption;
using AskAMuslimBackend.Application.Options.Commands.DeleteOption;
using AskAMuslimBackend.Application.Options.Commands.UpdateOption;
using AskAMuslimBackend.Application.Options.Queries;
using AskAMuslimBackend.Application.Options.Queries.GetOptionsByQuestion;
using AskAMuslimBackend.Domain.Constants;
using AskAMuslimBackend.Web.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AskAMuslimBackend.Web.Endpoints;

public class Options : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetOptionsByQuestion, "by-question/{questionId}").WithName("GetOptionsByQuestion").WithSummary("Get options for a question");
        groupBuilder.MapPost(CreateOption)
            .RequireAuthorization(Policies.ManageQuizzes)
            .WithName("CreateOption")
            .WithSummary("Create a new option");
        groupBuilder.MapPut(UpdateOption, "{id}")
            .RequireAuthorization(Policies.ManageQuizzes)
            .WithName("UpdateOption")
            .WithSummary("Update an option");
        groupBuilder.MapDelete(DeleteOption, "{id}")
            .RequireAuthorization(Policies.ManageQuizzes)
            .WithName("DeleteOption")
            .WithSummary("Delete an option");
    }

    public async Task<IResult> GetOptionsByQuestion(ISender sender, Guid questionId)
    {
        var result = await sender.Send(new GetOptionsByQuestionQuery(questionId));
        return result.ToHttpResult();
    }

    public async Task<IResult> CreateOption(ISender sender, CreateOptionCommand command)
    {
        var result = await sender.Send(command);
        return result.ToCreatedResult($"/{nameof(Options)}/{result.Data}");
    }

    public async Task<IResult> UpdateOption(ISender sender, Guid id, UpdateOptionCommand command)
    {
        var result = await sender.Send(command with { Id = id });
        return result.ToHttpResult();
    }

    public async Task<IResult> DeleteOption(ISender sender, Guid id)
    {
        var result = await sender.Send(new DeleteOptionCommand(id));
        return result.ToHttpResult();
    }
}
