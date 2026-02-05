using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Students.Commands.UpdateStudentStatus;
using AskAMuslimBackend.Application.Students.Queries;
using AskAMuslimBackend.Application.Students.Queries.GetAllStudents;
using AskAMuslimBackend.Application.Students.Queries.GetStudentProfile;
using AskAMuslimBackend.Application.Students.Queries.GetStudentDashboard;
using AskAMuslimBackend.Application.StudentProfiles.Commands.UpdateStudentProfile;
using AskAMuslimBackend.Domain.Constants;
using AskAMuslimBackend.Web.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AskAMuslimBackend.Web.Endpoints;

public class Students : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetStudentProfile, "{userId}")
            .RequireAuthorization()
            .WithName("GetStudentProfile")
            .WithSummary("Get student profile by user ID");

        groupBuilder.MapGet(GetAllStudents)
            .RequireAuthorization()
            .WithName("GetAllStudents")
            .WithSummary("Get all students (Admin only)");

        groupBuilder.MapGet(GetStudentDashboard, "dashboard")
            .RequireAuthorization(Policies.ViewAllUsers)
            .WithName("GetStudentDashboard")
            .WithSummary("Get student dashboard with progress tracking (Admin panel)");

        groupBuilder.MapPut(UpdateStudentProfile, "me")
            .RequireAuthorization()
            .WithName("UpdateStudentProfile")
            .WithSummary("Update current student's profile");

        groupBuilder.MapPut(UpdateStudentStatus, "{id}/status")
            .RequireAuthorization(Policies.ManageStudents)
            .WithName("UpdateStudentStatus")
            .WithSummary("Activate or deactivate a student (forces logout on deactivation)");
    }

    public async Task<IResult> GetStudentProfile(ISender sender, string userId)
    {
        var result = await sender.Send(new GetStudentProfileQuery { UserId = userId });
        return result.ToHttpResult();
    }

    public async Task<IResult> GetAllStudents(ISender sender, [AsParameters] GetAllStudentsQuery query)
    {
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    public async Task<IResult> GetStudentDashboard(ISender sender, [AsParameters] GetStudentDashboardQuery query)
    {
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    public async Task<IResult> UpdateStudentProfile(ISender sender, UpdateStudentProfileCommand command)
    {
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    public async Task<IResult> UpdateStudentStatus(ISender sender, Guid id, UpdateStudentStatusCommand command)
    {
        if (id != command.Id)
        {
            return Results.BadRequest(new { error = "ID mismatch" });
        }

        var result = await sender.Send(command);
        return result.ToHttpResult();
    }
}
