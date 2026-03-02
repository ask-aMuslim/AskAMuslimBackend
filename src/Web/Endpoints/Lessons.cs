using AskAMuslimBackend.Application.Lessons.Commands.CreateLesson;
using AskAMuslimBackend.Application.Lessons.Commands.DeleteLesson;
using AskAMuslimBackend.Application.Lessons.Commands.UpdateLesson;
using AskAMuslimBackend.Application.Lessons.Queries.GetLessonById;
using AskAMuslimBackend.Application.Lessons.Queries.GetLessonsWithPagination;
using AskAMuslimBackend.Domain.Constants;
using AskAMuslimBackend.Web.Infrastructure;

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
        groupBuilder.MapDelete(DeleteLesson, "{id}")
            .RequireAuthorization(Policies.ManageLessons)
            .WithName("DeleteLesson")
            .WithSummary("Delete a lesson");
    }

    public async Task<IResult> GetLessonsWithPagination(ISender sender, [AsParameters] GetLessonsWithPaginationQuery query, HttpRequest request)
    {
        var language = LanguageResolver.Resolve(request.Headers);
        var result = await sender.Send(query with { Language = language });
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

    public async Task<IResult> DeleteLesson(ISender sender, Guid id)
    {
        var result = await sender.Send(new DeleteLessonCommand(id));
        return result.ToHttpResult();
    }
}
