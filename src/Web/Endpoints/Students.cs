using AskAMuslimBackend.Application.Students.Commands.UpdateStudentProfile;
using AskAMuslimBackend.Application.Students.Queries;
using AskAMuslimBackend.Application.Students.Queries.GetAllStudents;
using AskAMuslimBackend.Application.Students.Queries.GetStudentProfile;
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

        groupBuilder.MapPut(UpdateStudentProfile, "me")
            .RequireAuthorization()
            .WithName("UpdateStudentProfile")
            .WithSummary("Update current student's profile");
    }

    public async Task<Results<Ok<StudentDto>, NotFound>> GetMyProfile(ISender sender)
    {
        var result = await sender.Send(new GetStudentProfileQuery());
        
        return result != null ? TypedResults.Ok(result) : TypedResults.NotFound();
    }

    public async Task<Results<Ok<StudentDto>, NotFound>> GetStudentProfile(ISender sender, string userId)
    {
        var result = await sender.Send(new GetStudentProfileQuery { UserId = userId });
        
        return result != null ? TypedResults.Ok(result) : TypedResults.NotFound();
    }

    public async Task<Ok<List<StudentDto>>> GetAllStudents(ISender sender)
    {
        var result = await sender.Send(new GetAllStudentsQuery());
        
        return TypedResults.Ok(result);
    }

    public async Task<Ok<Guid>> UpdateStudentProfile(ISender sender, UpdateStudentProfileCommand command)
    {
        var id = await sender.Send(command);
        
        return TypedResults.Ok(id);
    }
}
