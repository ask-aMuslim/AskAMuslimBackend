using AskAMuslimBackend.Application.Media.Commands.UploadImage;
using AskAMuslimBackend.Application.Media.Commands.UploadFile;
using AskAMuslimBackend.Web.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace AskAMuslimBackend.Web.Endpoints;

public class Media : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(UploadAttachment, "upload")
            .RequireAuthorization()
            .WithName("UploadAttachment")
            .WithSummary("Upload an attachment (image, video, audio, or document)")
            .WithDescription("Supports images, videos, audio, and documents. The file type is detected automatically.")
            .DisableAntiforgery(); // Required for file uploads
    }

    public async Task<IResult> UploadAttachment(
        ISender sender,
        IFormFile file,
        [FromQuery] string containerName = "attachments")
    {
        var command = new Application.Media.Commands.UploadAttachment.UploadAttachmentCommand
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
