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
        groupBuilder.MapPost(UploadImage, "upload")
            .RequireAuthorization()
            .WithName("UploadImage")
            .WithSummary("Upload an image for use in rich text content")
            .DisableAntiforgery(); // Required for file uploads

        groupBuilder.MapPost(UploadFile, "upload-file")
            .RequireAuthorization()
            .WithName("UploadFile")
            .WithSummary("Upload a file (image, video, audio, or document)")
            .WithDescription("Supports images (jpg, png, gif, webp, bmp, svg), videos (mp4, avi, mov, wmv, flv, mkv, webm, mpeg), audio (mp3, wav, aac, ogg, flac, m4a), and documents (pdf, doc, docx, xls, xlsx, ppt, pptx, txt, rtf, odt)")
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

    public async Task<IResult> UploadFile(
        ISender sender,
        IFormFile file,
        [FromQuery] string containerName = "lesson-files")
    {
        var command = new UploadFileCommand
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
