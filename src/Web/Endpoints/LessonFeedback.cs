using AskAMuslimBackend.Application.LessonFeedback.Commands.CreateLessonFeedback;
using AskAMuslimBackend.Application.LessonFeedback.Queries.GetLessonFeedbackById;
using AskAMuslimBackend.Application.LessonFeedback.Queries.GetLessonFeedbackByLesson;
using AskAMuslimBackend.Web.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AskAMuslimBackend.Web.Endpoints;

public class LessonFeedback : EndpointGroupBase
{
    public override string? GroupName => "LessonFeedback";

    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetById, "{id:guid}")
            .WithName("GetLessonFeedbackById")
            .WithSummary("Get lesson feedback by id");

        groupBuilder.MapGet(GetByLesson, "by-lesson/{lessonId:guid}")
            .WithName("GetLessonFeedbackByLesson")
            .WithSummary("Get lesson feedback by lesson id");

        groupBuilder.MapPost(Create)
            .RequireAuthorization()
            .WithName("CreateLessonFeedback")
            .WithSummary("Create or update current student's feedback for a lesson");
    }

    public async Task<IResult> GetById(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetLessonFeedbackByIdQuery(id));
        return result.ToHttpResult();
    }

    public async Task<IResult> GetByLesson(ISender sender, Guid lessonId)
    {
        var result = await sender.Send(new GetLessonFeedbackByLessonQuery(lessonId));
        return result.ToHttpResult();
    }

    public async Task<IResult> Create(ISender sender, CreateLessonFeedbackCommand command)
    {
        var result = await sender.Send(command);
        return result.ToCreatedResult($"/{nameof(LessonFeedback)}/{result.Data}");
    }
}
