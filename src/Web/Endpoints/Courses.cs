using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Courses.Commands.CreateCourse;
using AskAMuslimBackend.Application.Courses.Commands.DeleteCourse;
using AskAMuslimBackend.Application.Courses.Commands.UpdateCourse;
using AskAMuslimBackend.Application.Courses.Queries.GetCoursesWithPagination;
using AskAMuslimBackend.Application.Courses.Queries.GetCourseById;
using Microsoft.AspNetCore.Http.HttpResults;
using AskAMuslimBackend.Application.Courses.Queries;

namespace AskAMuslimBackend.Web.Endpoints;

public class Courses : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetCoursesWithPagination).WithName("GetCourses").WithSummary("Get course lookups and lists");
        groupBuilder.MapGet(GetCourseById, "{id}").WithName("GetCourse").WithSummary("Get a course by id");
        groupBuilder.MapPost(CreateCourse).RequireAuthorization().WithName("CreateCourse").WithSummary("Create a new course");
        groupBuilder.MapPut(UpdateCourse, "{id}").RequireAuthorization().WithName("UpdateCourse").WithSummary("Update a course");
        groupBuilder.MapDelete(DeleteCourse, "{id}").RequireAuthorization().WithName("DeleteCourse").WithSummary("Delete a course");
    }

    public async Task<Ok<PaginatedList<CourseBriefDto>>> GetCoursesWithPagination(ISender sender, [AsParameters] GetCoursesWithPaginationQuery query)
    {
        var result = await sender.Send(query);

        return TypedResults.Ok(result);
    }

    public async Task<Results<Ok<CourseBriefDto>, NotFound>> GetCourseById(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetCourseByIdQuery(id));

        return result != null ? TypedResults.Ok(result) : TypedResults.NotFound();
    }

    public async Task<Created<Guid>> CreateCourse(ISender sender, CreateCourseCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/{nameof(Courses)}/{id}", id);
    }

    public async Task<Results<NoContent, BadRequest>> UpdateCourse(ISender sender, Guid id, UpdateCourseCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();

        await sender.Send(command);

        return TypedResults.NoContent();
    }

    public async Task<NoContent> DeleteCourse(ISender sender, Guid id)
    {
        await sender.Send(new DeleteCourseCommand(id));
        return TypedResults.NoContent();
    }
}
