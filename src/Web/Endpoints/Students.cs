using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Students.Commands.UpdateStudentProfile;
using AskAMuslimBackend.Application.Students.Queries;
using AskAMuslimBackend.Application.Students.Queries.GetAllStudents;
using AskAMuslimBackend.Application.Students.Queries.GetStudentProfile;
using AskAMuslimBackend.Application.Students.Queries.GetStudentDashboard;
using AskAMuslimBackend.Domain.Constants;
using AskAMuslimBackend.Web.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AskAMuslimBackend.Web.Endpoints;

public class Students : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetMyProfile, "me")
            .RequireAuthorization()
            .WithName("GetMyStudentProfile")
            .WithSummary("Get current student's profile");

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
    }

    public async Task<IResult> GetMyProfile(ISender sender)
    {
        var result = await sender.Send(new GetStudentProfileQuery());
        return result.ToHttpResult();
    }

    public async Task<IResult> GetStudentProfile(ISender sender, string userId)
    {
        var result = await sender.Send(new GetStudentProfileQuery { UserId = userId });
        return result.ToHttpResult();
    }

    public async Task<IResult> GetAllStudents(ISender sender)
    {
        var result = await sender.Send(new GetAllStudentsQuery());
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
}
