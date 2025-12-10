using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Quizzes.Commands.CreateQuiz;
using AskAMuslimBackend.Application.Quizzes.Commands.DeleteQuiz;
using AskAMuslimBackend.Application.Quizzes.Commands.UpdateQuiz;
using AskAMuslimBackend.Application.Quizzes.Queries;
using AskAMuslimBackend.Application.Quizzes.Queries.GetQuizzesWithPagination;
using AskAMuslimBackend.Application.Quizzes.Queries.GetQuizById;
using AskAMuslimBackend.Web.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AskAMuslimBackend.Web.Endpoints;

public class Quizzes : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetQuizzesWithPagination).WithName("GetQuizzes").WithSummary("Get quizzes (optionally filtered by target)");
        groupBuilder.MapGet(GetQuizById, "{id}").WithName("GetQuiz").WithSummary("Get a quiz by id");
        groupBuilder.MapPost(CreateQuiz).RequireAuthorization().WithName("CreateQuiz").WithSummary("Create a new quiz");
        groupBuilder.MapPut(UpdateQuiz, "{id}").RequireAuthorization().WithName("UpdateQuiz").WithSummary("Update a quiz");
        groupBuilder.MapDelete(DeleteQuiz, "{id}").RequireAuthorization().WithName("DeleteQuiz").WithSummary("Delete a quiz");
    }

    public async Task<IResult> GetQuizzesWithPagination(ISender sender, [AsParameters] GetQuizzesWithPaginationQuery query)
    {
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    public async Task<IResult> GetQuizById(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetQuizByIdQuery(id));
        return result.ToHttpResult();
    }

    public async Task<IResult> CreateQuiz(ISender sender, CreateQuizCommand command)
    {
        var result = await sender.Send(command);
        return result.ToCreatedResult($"/{nameof(Quizzes)}/{result.Data}");
    }

    public async Task<IResult> UpdateQuiz(ISender sender, Guid id, UpdateQuizCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    public async Task<IResult> DeleteQuiz(ISender sender, Guid id)
    {
        var result = await sender.Send(new DeleteQuizCommand(id));
        return result.ToHttpResult();
    }
}
