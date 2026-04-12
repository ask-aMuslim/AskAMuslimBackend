using AskAMuslimBackend.Application.Enrollments.Commands.CreateEnrollment;
using AskAMuslimBackend.Application.Enrollments.Commands.DeleteEnrollment;
using AskAMuslimBackend.Application.Enrollments.Commands.UpdateEnrollmentStatus;
using AskAMuslimBackend.Application.Enrollments.Queries;
using AskAMuslimBackend.Application.Enrollments.Queries.GetEnrollmentById;
using AskAMuslimBackend.Application.Enrollments.Queries.GetEnrollmentsByCourse;
using AskAMuslimBackend.Application.Enrollments.Queries.GetEnrollmentsByStudent;
using AskAMuslimBackend.Application.Enrollments.Queries.GetUnCompletedEnrollments;
using AskAMuslimBackend.Domain.Constants;
using AskAMuslimBackend.Web.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AskAMuslimBackend.Web.Endpoints;

public class Enrollments : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetEnrollment, "{id:guid}").WithName("GetEnrollment").WithSummary("Get an enrollment by id");
        groupBuilder.MapGet(GetByCourse, "by-course/{courseId:guid}").WithName("GetEnrollmentsByCourse").WithSummary("Get enrollments by course id");
        groupBuilder.MapGet(GetByStudent, "my")
            .WithName("GetEnrollmentsByStudent")
            .WithSummary("Get enrollments for the current authenticated student");
        groupBuilder.MapPost(CreateEnrollment)
            .WithName("CreateEnrollment")
            .WithSummary("Create a new enrollment");
        groupBuilder.MapPut(UpdateEnrollmentStatus, "{id}/status")
            .WithName("UpdateEnrollmentStatus")
            .WithSummary("Update enrollment status");
        groupBuilder.MapDelete(DeleteEnrollment, "{id}")
            .RequireAuthorization(Policies.ManageAllCourses)
            .WithName("DeleteEnrollment")
            .WithSummary("Delete an enrollment");
        groupBuilder.MapGet(GetUnCompletedEnrollments, "uncompleted")
            .WithName("MyUcompletedCourses")
            .WithSummary("Get uncompleted enrollments for the current authenticated student");
    }

    public async Task<IResult> GetEnrollment(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetEnrollmentByIdQuery(id));
        return result.ToHttpResult();
    }

    public async Task<IResult> GetByCourse(ISender sender, Guid courseId)
    {
        var result = await sender.Send(new GetEnrollmentsByCourseQuery(courseId));
        return result.ToHttpResult();
    }

    public async Task<IResult> GetByStudent(ISender sender)
    {
        var result = await sender.Send(new GetEnrollmentsByStudentQuery());
        return result.ToHttpResult();
    }

    public async Task<IResult> CreateEnrollment(ISender sender, CreateEnrollmentCommand command)
    {
        var result = await sender.Send(command);
        return result.ToCreatedResult($"/{nameof(Enrollments)}/{result.Data}");
    }

    public async Task<IResult> UpdateEnrollmentStatus(ISender sender, Guid id, UpdateEnrollmentStatusCommand command)
    {
        var result = await sender.Send(command with { Id = id });
        return result.ToHttpResult();
    }

    public async Task<IResult> DeleteEnrollment(ISender sender, Guid id)
    {
        var result = await sender.Send(new DeleteEnrollmentCommand(id));
        return result.ToHttpResult();
    }

    public async Task<IResult> GetUnCompletedEnrollments(ISender sender)
    {
        var result = await sender.Send(new GetUnCompletedEnrollmentsQuery());
        return result.ToHttpResult();
    }
}
