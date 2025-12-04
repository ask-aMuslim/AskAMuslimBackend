using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Courses.Commands.CreateCourse;
using AskAMuslimBackend.Application.Courses.Commands.DeleteCourse;
using AskAMuslimBackend.Application.Courses.Commands.UpdateCourse;
using AskAMuslimBackend.Application.Courses.Queries.GetCoursesWithPagination;
using AskAMuslimBackend.Application.Courses.Queries.GetCourseById;
using AskAMuslimBackend.Web.Infrastructure;
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

    public async Task<IResult> GetCoursesWithPagination(ISender sender, [AsParameters] GetCoursesWithPaginationQuery query)
    {
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    public async Task<IResult> GetCourseById(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetCourseByIdQuery(id));
        return result.ToHttpResult();
    }

    public async Task<IResult> CreateCourse(ISender sender, CreateCourseCommand command)
    {
        var result = await sender.Send(command);
        return result.ToCreatedResult($"/{nameof(Courses)}/{result.Data}");
    }

    public async Task<IResult> UpdateCourse(ISender sender, Guid id, UpdateCourseCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    public async Task<IResult> DeleteCourse(ISender sender, Guid id)
    {
        var result = await sender.Send(new DeleteCourseCommand(id));
        return result.ToHttpResult();
    }
}
