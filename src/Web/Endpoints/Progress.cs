using AskAMuslimBackend.Application.Progress.Commands.CompleteLesson;
using AskAMuslimBackend.Application.Progress.Queries.GetCourseProgress;
using AskAMuslimBackend.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AskAMuslimBackend.Web.Endpoints;

public class Progress : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(CompleteLesson, "lesson/complete")
            .RequireAuthorization()
            .WithName("CompleteLesson")
            .WithSummary("Marks a lesson as complete for the current student, including video, audio, text, or document lessons");

        groupBuilder.MapGet(GetCourseProgress, "course/{courseId}")
            .RequireAuthorization()
            .WithName("GetCourseProgress")
            .WithSummary("Returns progress for a given course");
    }

    private async Task<bool> CompleteLesson(ISender sender, [FromBody] CompleteLessonCommand command)
    {
        return await sender.Send(command);
    }

    private async Task<CourseProgressDto> GetCourseProgress(ISender sender, Guid courseId)
    {
        return await sender.Send(new GetCourseProgressQuery(courseId));
    }
}