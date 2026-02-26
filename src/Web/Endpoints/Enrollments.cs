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
        groupBuilder.MapGet(GetEnrollment, "{id}").WithName("GetEnrollment").WithSummary("Get an enrollment by id");
        groupBuilder.MapGet(GetByCourse, "by-course/{courseId}").WithName("GetEnrollmentsByCourse").WithSummary("Get enrollments by course id");
        groupBuilder.MapGet(GetByStudent, "by-student/{studentId}")
            .RequireAuthorization()
            .WithName("GetEnrollmentsByStudent")
            .WithSummary("Get enrollments by student id (own enrollments or admin)");
        groupBuilder.MapPost(CreateEnrollment)
            .RequireAuthorization()
            .WithName("CreateEnrollment")
            .WithSummary("Create a new enrollment");
        groupBuilder.MapPut(UpdateEnrollmentStatus, "{id}/status")
            .RequireAuthorization()
            .WithName("UpdateEnrollmentStatus")
            .WithSummary("Update enrollment status");
        groupBuilder.MapDelete(DeleteEnrollment, "{id}")
            .RequireAuthorization(Policies.ManageAllCourses)
            .WithName("DeleteEnrollment")
            .WithSummary("Delete an enrollment");
        groupBuilder.MapGet(GetUnCompletedEnrollments, "{studentId}")
            .WithName("MyUcompletedCourses")
            .WithSummary("Get uncompleted enrollments for the current user")
            .RequireAuthorization();
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

    public async Task<IResult> GetByStudent(ISender sender, Guid studentId)
    {
        var result = await sender.Send(new GetEnrollmentsByStudentQuery(studentId));
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

    public async Task<IResult> GetUnCompletedEnrollments(ISender sender, Guid studentId)
    {
        var result = await sender.Send(new GetUnCompletedEnrollmentsQuery(studentId));
        return result.ToHttpResult();
    }
}
