using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Questions.Commands.CreateQuestion;
using AskAMuslimBackend.Application.Questions.Commands.DeleteQuestion;
using AskAMuslimBackend.Application.Questions.Commands.UpdateQuestion;
using AskAMuslimBackend.Application.Questions.Queries;
using AskAMuslimBackend.Application.Questions.Queries.GetQuestionsWithPagination;
using AskAMuslimBackend.Application.Questions.Queries.GetQuestionById;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AskAMuslimBackend.Web.Endpoints;

public class Questions : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetQuestionsWithPagination).WithName("GetQuestions").WithSummary("Get questions for a quiz");
        groupBuilder.MapGet(GetQuestionById, "{id}").WithName("GetQuestion").WithSummary("Get a question by id");
        groupBuilder.MapPost(CreateQuestion).RequireAuthorization().WithName("CreateQuestion").WithSummary("Create a new question");
        groupBuilder.MapPut(UpdateQuestion, "{id}").RequireAuthorization().WithName("UpdateQuestion").WithSummary("Update a question");
        groupBuilder.MapDelete(DeleteQuestion, "{id}").RequireAuthorization().WithName("DeleteQuestion").WithSummary("Delete a question");
    }

    public async Task<Ok<PaginatedList<QuestionBriefDto>>> GetQuestionsWithPagination(ISender sender, [AsParameters] GetQuestionsWithPaginationQuery query)
    {
        var result = await sender.Send(query);
        return TypedResults.Ok(result);
    }

    public async Task<Results<Ok<QuestionBriefDto>, NotFound>> GetQuestionById(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetQuestionByIdQuery(id));
        return result != null ? TypedResults.Ok(result) : TypedResults.NotFound();
    }

    public async Task<Created<Guid>> CreateQuestion(ISender sender, CreateQuestionCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/{nameof(Questions)}/{id}", id);
    }

    public async Task<Results<NoContent, BadRequest>> UpdateQuestion(ISender sender, Guid id, UpdateQuestionCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();
        await sender.Send(command);
        return TypedResults.NoContent();
    }

    public async Task<NoContent> DeleteQuestion(ISender sender, Guid id)
    {
        await sender.Send(new DeleteQuestionCommand(id));
        return TypedResults.NoContent();
    }
}
