using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Lessons.Commands.CreateLesson;
using AskAMuslimBackend.Application.Lessons.Commands.DeleteLesson;
using AskAMuslimBackend.Application.Lessons.Commands.UpdateLesson;
using AskAMuslimBackend.Application.Lessons.Queries;
using AskAMuslimBackend.Application.Lessons.Queries.GetLessonById;
using AskAMuslimBackend.Application.Lessons.Queries.GetLessonsWithPagination;
using AskAMuslimBackend.Web.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AskAMuslimBackend.Web.Endpoints;

public class Lessons : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetLessonsWithPagination).WithName("GetLessons").WithSummary("Get lessons (optionally filtered by course)");
        groupBuilder.MapGet(GetLessonById, "{id}").WithName("GetLesson").WithSummary("Get a lesson by id");
        groupBuilder.MapPost(CreateLesson).RequireAuthorization().WithName("CreateLesson").WithSummary("Create a new lesson");
        groupBuilder.MapPut(UpdateLesson, "{id}").RequireAuthorization().WithName("UpdateLesson").WithSummary("Update a lesson");
        groupBuilder.MapDelete(DeleteLesson, "{id}").RequireAuthorization().WithName("DeleteLesson").WithSummary("Delete a lesson");
    }

    public async Task<Ok<PaginatedList<LessonBriefDto>>> GetLessonsWithPagination(ISender sender, [AsParameters] GetLessonsWithPaginationQuery query)
    {
        var result = await sender.Send(query);
        return TypedResults.Ok(result);
    }

    public async Task<Results<Ok<LessonBriefDto>, NotFound>> GetLessonById(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetLessonByIdQuery(id));
        return result != null ? TypedResults.Ok(result) : TypedResults.NotFound();
    }

    public async Task<IResult> CreateLesson(ISender sender, CreateLessonCommand command)
    {
        var result = await sender.Send(command);
        return result.ToCreatedResult($"/{nameof(Lessons)}/{result.Data}");
    }

    public async Task<Results<NoContent, BadRequest>> UpdateLesson(ISender sender, Guid id, UpdateLessonCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();
        await sender.Send(command);
        return TypedResults.NoContent();
    }

    public async Task<NoContent> DeleteLesson(ISender sender, Guid id)
    {
        await sender.Send(new DeleteLessonCommand(id));
        return TypedResults.NoContent();
    }
}
