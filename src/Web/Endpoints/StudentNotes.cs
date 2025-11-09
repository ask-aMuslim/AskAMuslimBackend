using AskAMuslimBackend.Application.StudentNotes.Commands.CreateStudentNote;
using AskAMuslimBackend.Application.StudentNotes.Commands.DeleteStudentNote;
using AskAMuslimBackend.Application.StudentNotes.Queries;
using AskAMuslimBackend.Application.StudentNotes.Queries.GetStudentNoteById;
using AskAMuslimBackend.Application.StudentNotes.Queries.GetStudentNotesByLesson;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AskAMuslimBackend.Web.Endpoints;

public class StudentNotes : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetById, "{id}").WithName("GetStudentNote").WithSummary("Get a student note by id");
        groupBuilder.MapGet(GetByLesson, "by-lesson/{lessonId}").WithName("GetStudentNotesByLesson").WithSummary("Get student notes for a lesson");
        groupBuilder.MapPost(CreateNote).RequireAuthorization().WithName("CreateStudentNote").WithSummary("Create a new student note");
        groupBuilder.MapDelete(DeleteNote, "{id}").RequireAuthorization().WithName("DeleteStudentNote").WithSummary("Delete a student note");
    }

    public async Task<Results<Ok<StudentNoteDto>, NotFound>> GetById(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetStudentNoteByIdQuery(id));
        return result != null ? TypedResults.Ok(result) : TypedResults.NotFound();
    }

    public async Task<Ok<List<StudentNoteDto>>> GetByLesson(ISender sender, Guid lessonId)
    {
        var result = await sender.Send(new GetStudentNotesByLessonQuery(lessonId));
        return TypedResults.Ok(result);
    }

    public async Task<Created<Guid>> CreateNote(ISender sender, CreateStudentNoteCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/{nameof(StudentNotes)}/{id}", id);
    }

    public async Task<NoContent> DeleteNote(ISender sender, Guid id)
    {
        await sender.Send(new DeleteStudentNoteCommand(id));
        return TypedResults.NoContent();
    }
}
