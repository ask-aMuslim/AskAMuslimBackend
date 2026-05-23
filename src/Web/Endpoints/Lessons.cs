using AskAMuslimBackend.Application.Lessons.Commands.CreateLesson;
using AskAMuslimBackend.Application.Lessons.Commands.DeleteLesson;
using AskAMuslimBackend.Application.Lessons.Commands.SetLessonDuration;
using AskAMuslimBackend.Application.Lessons.Commands.UpdateLesson;
using AskAMuslimBackend.Application.Lessons.Queries.GetLessonById;
using AskAMuslimBackend.Application.Lessons.Queries.GetLessonsWithPagination;
using AskAMuslimBackend.Domain.Constants;

namespace AskAMuslimBackend.Web.Endpoints;

public class Lessons : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetLessonsWithPagination).WithName("GetLessons").WithSummary("Get lessons (optionally filtered by course)");
        groupBuilder.MapGet(GetLessonById, "{id}").WithName("GetLesson").WithSummary("Get a lesson by id");
        groupBuilder.MapPost(CreateLesson)
            .RequireAuthorization(Policies.ManageLessons)
            .WithName("CreateLesson")
            .WithSummary("Create a new lesson");
        groupBuilder.MapPut(UpdateLesson, "{id}")
            .RequireAuthorization(Policies.ManageLessons)
            .WithName("UpdateLesson")
            .WithSummary("Update a lesson");
        groupBuilder.MapPut(SetDuration, "{id}/duration")
            .RequireAuthorization(Policies.ManageLessons)
            .WithName("SetLessonDuration")
            .WithSummary("Auto-resolve or manually set lesson duration. Auto for Video/Audio; manual override optional.");
        groupBuilder.MapDelete(DeleteLesson, "{id}")
            .RequireAuthorization(Policies.ManageLessons)
            .WithName("DeleteLesson")
            .WithSummary("Delete a lesson");
    }

    public async Task<IResult> GetLessonsWithPagination(ISender sender, [AsParameters] GetLessonsWithPaginationQuery query)
    {
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    public async Task<IResult> GetLessonById(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetLessonByIdQuery(id));
        return result.ToHttpResult();
    }

    public async Task<IResult> CreateLesson(ISender sender, CreateLessonCommand command)
    {
        var result = await sender.Send(command);
        return result.ToCreatedResult($"/{nameof(Lessons)}/{result.Data}");
    }

    public async Task<IResult> UpdateLesson(ISender sender, Guid id, UpdateLessonCommand command)
    {
        var result = await sender.Send(command with { Id = id });
        return result.ToHttpResult();
    }

    public async Task<IResult> SetDuration(ISender sender, Guid id, SetLessonDurationCommand command)
    {
        var result = await sender.Send(command with { LessonId = id });
        return result.ToHttpResult();
    }

    public async Task<IResult> DeleteLesson(ISender sender, Guid id)
    {
        var result = await sender.Send(new DeleteLessonCommand(id));
        return result.ToHttpResult();
    }
}
