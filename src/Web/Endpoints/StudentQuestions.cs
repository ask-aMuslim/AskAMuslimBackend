using AskAMuslimBackend.Application.StudentQuestions.Commands.CreateStudentQuestion;
using AskAMuslimBackend.Application.StudentQuestions.Commands.DeleteStudentQuestion;
using AskAMuslimBackend.Application.StudentQuestions.Commands.AnswerStudentQuestion;
using AskAMuslimBackend.Application.StudentQuestions.Queries;
using AskAMuslimBackend.Application.StudentQuestions.Queries.GetStudentQuestionById;
using AskAMuslimBackend.Application.StudentQuestions.Queries.GetStudentQuestionsByLesson;
using GetStudentQuestionsNS = AskAMuslimBackend.Application.StudentQuestions.Queries.GetStudentQuestions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace AskAMuslimBackend.Web.Endpoints;

public class StudentQuestions : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetById, "{id}").WithName("GetStudentQuestion").WithSummary("Get a student question by id");
        groupBuilder.MapGet(GetByLesson, "by-lesson/{lessonId}").WithName("GetStudentQuestionsByLesson").WithSummary("Get student questions for a lesson");
        groupBuilder.MapGet(GetByStudent, "by-student/{studentId}").WithName("GetStudentQuestionsByStudent").WithSummary("Get all questions from a student");
        groupBuilder.MapPost(CreateQuestion).RequireAuthorization().WithName("CreateStudentQuestion").WithSummary("Create a new student question");
        groupBuilder.MapPut(AnswerQuestion, "{id}/answer").RequireAuthorization().WithName("AnswerStudentQuestion").WithSummary("Answer a student question");
        groupBuilder.MapDelete(DeleteQuestion, "{id}").RequireAuthorization().WithName("DeleteStudentQuestion").WithSummary("Delete a student question");
    }

    public async Task<Results<Ok<StudentQuestionDto>, NotFound>> GetById(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetStudentQuestionByIdQuery(id));
        return result != null ? TypedResults.Ok(result) : TypedResults.NotFound();
    }

    public async Task<Ok<List<StudentQuestionDto>>> GetByLesson(ISender sender, Guid lessonId)
    {
        var result = await sender.Send(new GetStudentQuestionsByLessonQuery(lessonId));
        return TypedResults.Ok(result);
    }

    public async Task<Ok<List<GetStudentQuestionsNS.StudentQuestionDto>>> GetByStudent(ISender sender, Guid studentId)
    {
        var result = await sender.Send(new GetStudentQuestionsNS.GetStudentQuestionsQuery { StudentId = studentId });
        return TypedResults.Ok(result);
    }

    public async Task<Created<Guid>> CreateQuestion(ISender sender, CreateStudentQuestionCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/{nameof(StudentQuestions)}/{id}", id);
    }

    public async Task<Results<NoContent, BadRequest>> AnswerQuestion(ISender sender, Guid id, [FromBody] AnswerStudentQuestionCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();
        await sender.Send(command);
        return TypedResults.NoContent();
    }

    public async Task<NoContent> DeleteQuestion(ISender sender, Guid id)
    {
        await sender.Send(new DeleteStudentQuestionCommand(id));
        return TypedResults.NoContent();
    }
}
