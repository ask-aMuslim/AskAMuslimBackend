using AskAMuslimBackend.Application.Courses.Commands.AddCoursePrerequisite;
using AskAMuslimBackend.Application.Courses.Commands.CreateCourse;
using AskAMuslimBackend.Application.Courses.Commands.DeleteCourse;
using AskAMuslimBackend.Application.Courses.Commands.RemoveCoursePrerequisite;
using AskAMuslimBackend.Application.Courses.Commands.UpdateCourse;
using AskAMuslimBackend.Application.Courses.Queries.GetCourseById;
using AskAMuslimBackend.Application.Courses.Queries.GetCourseDetailById;
using AskAMuslimBackend.Application.Courses.Queries.GetCourseRoadmap;
using AskAMuslimBackend.Application.Courses.Queries.GetCoursesWithPagination;
using AskAMuslimBackend.Domain.Constants;
using AskAMuslimBackend.Web.Infrastructure;

namespace AskAMuslimBackend.Web.Endpoints;

public class Courses : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetCoursesWithLevelId).WithName("GetCourses").WithSummary("Get courses (optionally filtered by level)");
        groupBuilder.MapGet(GetCourseById, "{id}").WithName("GetCourse").WithSummary("Get a course by id");
        groupBuilder.MapGet(GetCourseDetailById, "{id}/detail").WithName("GetCourseDetail").WithSummary("Get detailed course info with prerequisites");
        groupBuilder.MapGet(GetCourseRoadmap, "roadmap/{levelId}").WithName("GetCourseRoadmap").WithSummary("Get complete course roadmap for a level");
        groupBuilder.MapPost(CreateCourse)
            .RequireAuthorization(Policies.CreateCourse)
            .WithName("CreateCourse")
            .WithSummary("Create a new course");
        groupBuilder.MapPut(UpdateCourse, "{id}")
            .RequireAuthorization(Policies.ManageAllCourses)
            .WithName("UpdateCourse")
            .WithSummary("Update a course");
        groupBuilder.MapDelete(DeleteCourse, "{id}")
            .RequireAuthorization(Policies.ManageAllCourses)
            .WithName("DeleteCourse")
            .WithSummary("Delete a course");
        groupBuilder.MapPost(AddPrerequisite, "{id}/prerequisites")
            .RequireAuthorization(Policies.ManageAllCourses)
            .WithName("AddCoursePrerequisite")
            .WithSummary("Add a prerequisite to a course");
        groupBuilder.MapDelete(RemovePrerequisite, "{id}/prerequisites/{prerequisiteId}")
            .RequireAuthorization(Policies.ManageAllCourses)
            .WithName("RemoveCoursePrerequisite")
            .WithSummary("Remove a prerequisite from a course");
    }

    public async Task<IResult> GetCoursesWithLevelId(ISender sender, [AsParameters] GetCoursesWithPaginationQuery query, HttpRequest request)
    {
        var language = LanguageResolver.Resolve(request.Headers);
        var result = await sender.Send(query with { Language = language });
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
        var result = await sender.Send(command with { Id = id });
        return result.ToHttpResult();
    }

    public async Task<IResult> DeleteCourse(ISender sender, Guid id)
    {
        var result = await sender.Send(new DeleteCourseCommand(id));
        return result.ToHttpResult();
    }

    public async Task<IResult> GetCourseDetailById(ISender sender, Guid id, Guid? studentId = null)
    {
        var result = await sender.Send(new GetCourseDetailByIdQuery(id, studentId));
        return result.ToHttpResult();
    }

    public async Task<IResult> GetCourseRoadmap(ISender sender, Guid levelId, Guid? studentId = null)
    {
        var result = await sender.Send(new GetCourseRoadmapQuery(levelId, studentId));
        return result.ToHttpResult();
    }

    public async Task<IResult> AddPrerequisite(ISender sender, Guid id, AddCoursePrerequisiteCommand command)
    {
        var result = await sender.Send(command with { CourseId = id });
        return result.ToHttpResult();
    }

    public async Task<IResult> RemovePrerequisite(ISender sender, Guid id, Guid prerequisiteId)
    {
        var result = await sender.Send(new RemoveCoursePrerequisiteCommand
        {
            CourseId = id,
            PrerequisiteCourseId = prerequisiteId
        });
        return result.ToHttpResult();
    }
}
