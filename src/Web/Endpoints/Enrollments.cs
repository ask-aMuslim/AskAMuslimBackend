using AskAMuslimBackend.Application.Enrollments.Commands.CreateEnrollment;
using AskAMuslimBackend.Application.Enrollments.Commands.DeleteEnrollment;
using AskAMuslimBackend.Application.Enrollments.Commands.UpdateEnrollmentStatus;
using AskAMuslimBackend.Application.Enrollments.Queries;
using AskAMuslimBackend.Application.Enrollments.Queries.GetEnrollmentById;
using AskAMuslimBackend.Application.Enrollments.Queries.GetEnrollmentsByCourse;
using AskAMuslimBackend.Application.Enrollments.Queries.GetEnrollmentsByStudent;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AskAMuslimBackend.Web.Endpoints;

public class Enrollments : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetEnrollment, "{id}").WithName("GetEnrollment").WithSummary("Get an enrollment by id");
        groupBuilder.MapGet(GetByCourse, "by-course/{courseId}").WithName("GetEnrollmentsByCourse").WithSummary("Get enrollments by course id");
        groupBuilder.MapGet(GetByStudent, "by-student/{studentId}").RequireAuthorization().WithName("GetEnrollmentsByStudent").WithSummary("Get enrollments by student id");
        groupBuilder.MapPost(CreateEnrollment).RequireAuthorization().WithName("CreateEnrollment").WithSummary("Create a new enrollment");
        groupBuilder.MapPut(UpdateEnrollmentStatus, "{id}/status").RequireAuthorization().WithName("UpdateEnrollmentStatus").WithSummary("Update enrollment status");
        groupBuilder.MapDelete(DeleteEnrollment, "{id}").RequireAuthorization().WithName("DeleteEnrollment").WithSummary("Delete an enrollment");
    }

    public async Task<Results<Ok<EnrollmentDto>, NotFound>> GetEnrollment(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetEnrollmentByIdQuery(id));
        return result != null ? TypedResults.Ok(result) : TypedResults.NotFound();
    }

    public async Task<Ok<List<EnrollmentDto>>> GetByCourse(ISender sender, Guid courseId)
    {
        var result = await sender.Send(new GetEnrollmentsByCourseQuery(courseId));
        return TypedResults.Ok(result);
    }

    public async Task<Ok<List<EnrollmentDto>>> GetByStudent(ISender sender, Guid studentId)
    {
        var result = await sender.Send(new GetEnrollmentsByStudentQuery(studentId));
        return TypedResults.Ok(result);
    }

    public async Task<Created<Guid>> CreateEnrollment(ISender sender, CreateEnrollmentCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/{nameof(Enrollments)}/{id}", id);
    }

    public async Task<Results<NoContent, BadRequest>> UpdateEnrollmentStatus(ISender sender, Guid id, UpdateEnrollmentStatusCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();
        await sender.Send(command);
        return TypedResults.NoContent();
    }

    public async Task<NoContent> DeleteEnrollment(ISender sender, Guid id)
    {
        await sender.Send(new DeleteEnrollmentCommand(id));
        return TypedResults.NoContent();
    }
}
