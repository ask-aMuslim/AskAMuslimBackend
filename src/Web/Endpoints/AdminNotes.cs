using AskAMuslimBackend.Application.AdminNotes.Commands.CreateAdminNote;
using AskAMuslimBackend.Application.AdminNotes.Queries.GetAdminNotes;
using AskAMuslimBackend.Domain.Constants;
using AskAMuslimBackend.Web.Infrastructure;

namespace AskAMuslimBackend.Web.Endpoints;

public class AdminNotes : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder app)
    {
        app.MapGet(GetAdminNotes, "{studentId}").RequireAuthorization();
        app.MapPost(CreateAdminNote).RequireAuthorization();
    }

    public async Task<IResult> GetAdminNotes(ISender sender, Guid studentId)
    {
        var result = await sender.Send(new GetAdminNotesQuery { StudentId = studentId });
        return result.ToHttpResult();
    }

    public async Task<IResult> CreateAdminNote(ISender sender, CreateAdminNoteCommand command)
    {
        var result = await sender.Send(command);
        return result.ToCreatedResult($"/AdminNotes/{result.Data}");
    }
}
