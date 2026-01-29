using AskAMuslimBackend.Application.StudentProfiles;
using AskAMuslimBackend.Application.StudentProfiles.Commands.CreateStudentProfile;
using AskAMuslimBackend.Application.StudentProfiles.Commands.UpdateStudentProfile;
using AskAMuslimBackend.Application.StudentProfiles.Queries.GetStudentProfile;
using AskAMuslimBackend.Domain.Constants;
using AskAMuslimBackend.Web.Infrastructure;

namespace AskAMuslimBackend.Web.Endpoints;

public class StudentProfiles : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetMyProfile, "me")
            .RequireAuthorization()
            .WithName("GetMyStudentProfileDetails")
            .WithSummary("Get current student's profile with religious status information");

        groupBuilder.MapGet(GetStudentProfile, "{userId}")
            .RequireAuthorization(Policies.ViewAllUsers)
            .WithName("GetStudentProfileByUserId")
            .WithSummary("Get student profile by user ID (Admin only)");

        groupBuilder.MapPost(CreateStudentProfile, "me")
            .RequireAuthorization()
            .WithName("CreateStudentProfile")
            .WithSummary("Create current student's profile");

        groupBuilder.MapPut(UpdateStudentProfile, "me")
            .RequireAuthorization()
            .WithName("UpdateStudentProfileDetails")
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

    public async Task<IResult> CreateStudentProfile(ISender sender, CreateStudentProfileCommand command)
    {
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    public async Task<IResult> UpdateStudentProfile(ISender sender, UpdateStudentProfileCommand command)
    {
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }
}
