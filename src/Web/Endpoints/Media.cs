using AskAMuslimBackend.Application.Media.Commands.UploadImage;
using AskAMuslimBackend.Web.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace AskAMuslimBackend.Web.Endpoints;

public class Media : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(UploadImage, "upload")
            .RequireAuthorization()
            .WithName("UploadImage")
            .WithSummary("Upload an image for use in rich text content")
            .DisableAntiforgery(); // Required for file uploads
    }

    public async Task<IResult> UploadImage(
        ISender sender,
        IFormFile file,
        [FromQuery] string containerName = "lesson-images")
    {
        var command = new UploadImageCommand
        {
            File = file,
            ContainerName = containerName
        };

        var result = await sender.Send(command);

        if (result.Succeeded)
        {
            return TypedResults.Ok(new { url = result.Data });
        }

        return result.ToHttpResult();
    }
}
