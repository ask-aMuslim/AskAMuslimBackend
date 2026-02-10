using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Questions.Commands.CreateQuestion;
using AskAMuslimBackend.Application.Questions.Commands.DeleteQuestion;
using AskAMuslimBackend.Application.Questions.Commands.UpdateQuestion;
using AskAMuslimBackend.Application.Questions.Queries;
using AskAMuslimBackend.Application.Questions.Queries.GetQuestionById;
using AskAMuslimBackend.Application.Questions.Queries.GetQuestionsWithPagination;
using AskAMuslimBackend.Domain.Constants;
using AskAMuslimBackend.Web.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AskAMuslimBackend.Web.Endpoints;

public class Questions : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetQuestionsWithPagination).WithName("GetQuestions").WithSummary("Get questions for a quiz");
        groupBuilder.MapGet(GetQuestionById, "{id}").WithName("GetQuestion").WithSummary("Get a question by id");
        groupBuilder.MapPost(CreateQuestion)
            .RequireAuthorization(Policies.ManageQuizzes)
            .WithName("CreateQuestion")
            .WithSummary("Create a new question");
        groupBuilder.MapPut(UpdateQuestion, "{id}")
            .RequireAuthorization(Policies.ManageQuizzes)
            .WithName("UpdateQuestion")
            .WithSummary("Update a question");
        groupBuilder.MapDelete(DeleteQuestion, "{id}")
            .RequireAuthorization(Policies.ManageQuizzes)
            .WithName("DeleteQuestion")
            .WithSummary("Delete a question");
    }

    public async Task<IResult> GetQuestionsWithPagination(ISender sender, [AsParameters] GetQuestionsWithPaginationQuery query)
    {
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    public async Task<IResult> GetQuestionById(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetQuestionByIdQuery(id));
        return result.ToHttpResult();
    }

    public async Task<IResult> CreateQuestion(ISender sender, CreateQuestionCommand command)
    {
        var result = await sender.Send(command);
        return result.ToCreatedResult($"/{nameof(Questions)}/{result.Data}");
    }

    public async Task<IResult> UpdateQuestion(ISender sender, Guid id, UpdateQuestionCommand command)
    {
        var result = await sender.Send(command with { Id = id });
        return result.ToHttpResult();
    }

    public async Task<IResult> DeleteQuestion(ISender sender, Guid id)
    {
        var result = await sender.Send(new DeleteQuestionCommand(id));
        return result.ToHttpResult();
    }
}
