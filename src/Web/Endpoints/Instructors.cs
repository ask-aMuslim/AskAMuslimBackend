using AskAMuslimBackend.Application.Instructors.Commands.UpdateInstructorProfile;
using AskAMuslimBackend.Application.Instructors.Queries;
using AskAMuslimBackend.Application.Instructors.Queries.GetAllInstructors;
using AskAMuslimBackend.Application.Instructors.Queries.GetInstructorProfile;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AskAMuslimBackend.Web.Endpoints;

public class Instructors : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetMyProfile, "me")
            .RequireAuthorization()
            .WithName("GetMyInstructorProfile")
            .WithSummary("Get current instructor's profile");

        groupBuilder.MapGet(GetInstructorProfile, "{userId}")
            .RequireAuthorization()
            .WithName("GetInstructorProfile")
            .WithSummary("Get instructor profile by user ID");

        groupBuilder.MapGet(GetAllInstructors)
            .WithName("GetAllInstructors")
            .WithSummary("Get all active instructors");

        groupBuilder.MapPut(UpdateInstructorProfile, "me")
            .RequireAuthorization()
            .WithName("UpdateInstructorProfile")
            .WithSummary("Update current instructor's profile");
    }

    public async Task<Results<Ok<InstructorDto>, NotFound>> GetMyProfile(ISender sender)
    {
        var result = await sender.Send(new GetInstructorProfileQuery());
        
        return result != null ? TypedResults.Ok(result) : TypedResults.NotFound();
    }

    public async Task<Results<Ok<InstructorDto>, NotFound>> GetInstructorProfile(ISender sender, string userId)
    {
        var result = await sender.Send(new GetInstructorProfileQuery { UserId = userId });
        
        return result != null ? TypedResults.Ok(result) : TypedResults.NotFound();
    }

    public async Task<Ok<List<InstructorDto>>> GetAllInstructors(ISender sender)
    {
        var result = await sender.Send(new GetAllInstructorsQuery());
        
        return TypedResults.Ok(result);
    }

    public async Task<Ok<Guid>> UpdateInstructorProfile(ISender sender, UpdateInstructorProfileCommand command)
    {
        var id = await sender.Send(command);
        
        return TypedResults.Ok(id);
    }
}
