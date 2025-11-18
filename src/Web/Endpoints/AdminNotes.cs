using AskAMuslimBackend.Application.AdminNotes.Commands.CreateAdminNote;
using AskAMuslimBackend.Application.AdminNotes.Queries.GetAdminNotes;
using AskAMuslimBackend.Domain.Constants;

namespace AskAMuslimBackend.Web.Endpoints;

public class AdminNotes : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder app)
    {
        app.MapGet(GetAdminNotes, "{studentId}").RequireAuthorization();
        app.MapPost(CreateAdminNote).RequireAuthorization();
    }

    public async Task<List<AdminNoteDto>> GetAdminNotes(ISender sender, Guid studentId)
    {
        return await sender.Send(new GetAdminNotesQuery { StudentId = studentId });
    }

    public async Task<Guid> CreateAdminNote(ISender sender, CreateAdminNoteCommand command)
    {
        return await sender.Send(command);
    }
}
