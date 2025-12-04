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

    public async Task<Ok<PaginatedList<QuizBriefDto>>> GetQuizzesWithPagination(ISender sender, [AsParameters] GetQuizzesWithPaginationQuery query)
    {
        var result = await sender.Send(query);
        return TypedResults.Ok(result);
    }

    public async Task<Results<Ok<QuizBriefDto>, NotFound>> GetQuizById(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetQuizByIdQuery(id));
        return result != null ? TypedResults.Ok(result) : TypedResults.NotFound();
    }

    public async Task<IResult> CreateQuiz(ISender sender, CreateQuizCommand command)
    {
        var result = await sender.Send(command);
        return result.ToCreatedResult($"/{nameof(Quizzes)}/{result.Data}");
    }

    public async Task<Results<NoContent, BadRequest>> UpdateQuiz(ISender sender, Guid id, UpdateQuizCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();
        await sender.Send(command);
        return TypedResults.NoContent();
    }

    public async Task<NoContent> DeleteQuiz(ISender sender, Guid id)
    {
        await sender.Send(new DeleteQuizCommand(id));
        return TypedResults.NoContent();
    }
}
