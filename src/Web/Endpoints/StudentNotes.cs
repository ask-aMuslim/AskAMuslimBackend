using AskAMuslimBackend.Application.StudentNotes.Commands.CreateStudentNote;
using AskAMuslimBackend.Application.StudentNotes.Commands.DeleteStudentNote;
using AskAMuslimBackend.Application.StudentNotes.Queries;
using AskAMuslimBackend.Application.StudentNotes.Queries.GetStudentNoteById;
using AskAMuslimBackend.Application.StudentNotes.Queries.GetStudentNotesByLesson;
using AskAMuslimBackend.Web.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AskAMuslimBackend.Web.Endpoints;

public class StudentNotes : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetById, "{id}").WithName("GetStudentNote").WithSummary("Get a student note by id");
        groupBuilder.MapGet(GetByLesson, "by-lesson/{lessonId}").WithName("GetStudentNotesByLesson").WithSummary("Get student notes for a lesson");
        groupBuilder.MapPost(CreateNote)
            .RequireAuthorization()
            .WithName("CreateStudentNote")
            .WithSummary("Create a new student note");
        groupBuilder.MapDelete(DeleteNote, "{id}")
            .RequireAuthorization()
            .WithName("DeleteStudentNote")
            .WithSummary("Delete a student note");
    }

    public async Task<IResult> GetById(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetStudentNoteByIdQuery(id));
        return result.ToHttpResult();
    }

    public async Task<IResult> GetByLesson(ISender sender, Guid lessonId)
    {
        var result = await sender.Send(new GetStudentNotesByLessonQuery(lessonId));
        return result.ToHttpResult();
    }

    public async Task<IResult> CreateNote(ISender sender, CreateStudentNoteCommand command)
    {
        var result = await sender.Send(command);
        return result.ToCreatedResult($"/{nameof(StudentNotes)}/{result.Data}");
    }

    public async Task<IResult> DeleteNote(ISender sender, Guid id)
    {
        var result = await sender.Send(new DeleteStudentNoteCommand(id));
        return result.ToHttpResult();
    }
}
