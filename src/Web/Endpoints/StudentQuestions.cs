using AskAMuslimBackend.Application.StudentQuestions.Commands.CreateStudentQuestion;
using AskAMuslimBackend.Application.StudentQuestions.Commands.DeleteStudentQuestion;
using AskAMuslimBackend.Application.StudentQuestions.Commands.AnswerStudentQuestion;
using AskAMuslimBackend.Application.StudentQuestions.Queries;
using AskAMuslimBackend.Application.StudentQuestions.Queries.GetStudentQuestionById;
using AskAMuslimBackend.Application.StudentQuestions.Queries.GetStudentQuestionsByLesson;
using AskAMuslimBackend.Web.Infrastructure;
using GetStudentQuestionsNS = AskAMuslimBackend.Application.StudentQuestions.Queries.GetStudentQuestions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using AskAMuslimBackend.Domain.Constants;

namespace AskAMuslimBackend.Web.Endpoints;

public class StudentQuestions : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetById, "{id}").WithName("GetStudentQuestion").WithSummary("Get a student question by id");
        groupBuilder.MapGet(GetByLesson, "by-lesson/{lessonId}").WithName("GetStudentQuestionsByLesson").WithSummary("Get student questions for a lesson");
        groupBuilder.MapGet(GetByStudent, "by-student/{studentId}").WithName("GetStudentQuestionsByStudent").WithSummary("Get all questions from a student");
        groupBuilder.MapPost(CreateQuestion)
            .RequireAuthorization()
            .WithName("CreateStudentQuestion")
            .WithSummary("Create a new student question");
        groupBuilder.MapPut(AnswerQuestion, "{id}/answer")
            .RequireAuthorization(Policies.ViewAllStudents)
            .WithName("AnswerStudentQuestion")
            .WithSummary("Answer a student question");
        groupBuilder.MapDelete(DeleteQuestion, "{id}")
            .RequireAuthorization()
            .WithName("DeleteStudentQuestion")
            .WithSummary("Delete a student question");
    }

    public async Task<IResult> GetById(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetStudentQuestionByIdQuery(id));
        return result.ToHttpResult();
    }

    public async Task<IResult> GetByLesson(ISender sender, Guid lessonId)
    {
        var result = await sender.Send(new GetStudentQuestionsByLessonQuery(lessonId));
        return result.ToHttpResult();
    }

    public async Task<IResult> GetByStudent(ISender sender, Guid studentId)
    {
        var result = await sender.Send(new GetStudentQuestionsNS.GetStudentQuestionsQuery { StudentId = studentId });
        return result.ToHttpResult();
    }

    public async Task<IResult> CreateQuestion(ISender sender, CreateStudentQuestionCommand command)
    {
        var result = await sender.Send(command);
        return result.ToCreatedResult($"/{nameof(StudentQuestions)}/{result.Data}");
    }

    public async Task<IResult> AnswerQuestion(ISender sender, Guid id, [FromBody] AnswerStudentQuestionCommand command)
    {
        var result = await sender.Send(command with { Id = id });
        return result.ToHttpResult();
    }

    public async Task<IResult> DeleteQuestion(ISender sender, Guid id)
    {
        var result = await sender.Send(new DeleteStudentQuestionCommand(id));
        return result.ToHttpResult();
    }
}
