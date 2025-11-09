using AskAMuslimBackend.Application.QuizAttempts.Commands.CompleteQuizAttempt;
using AskAMuslimBackend.Application.QuizAttempts.Commands.CreateQuizAttempt;
using AskAMuslimBackend.Application.QuizAttempts.Queries;
using AskAMuslimBackend.Application.QuizAttempts.Queries.GetQuizAttemptsByQuiz;
using AskAMuslimBackend.Application.QuizAttempts.Queries.GetQuizAttemptsByStudent;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AskAMuslimBackend.Web.Endpoints;

public class QuizAttempts : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetForQuiz, "by-quiz/{quizId}").WithName("GetQuizAttemptsByQuiz").WithSummary("Get quiz attempts for a quiz");
        groupBuilder.MapGet(GetForStudent, "by-student/{studentId}").RequireAuthorization().WithName("GetQuizAttemptsByStudent").WithSummary("Get quiz attempts for a student");
        groupBuilder.MapPost(StartAttempt).RequireAuthorization().WithName("StartQuizAttempt").WithSummary("Start a new quiz attempt");
        groupBuilder.MapPut(CompleteAttempt, "complete/{attemptId}").RequireAuthorization().WithName("CompleteQuizAttempt").WithSummary("Complete a quiz attempt");
    }

    public async Task<Ok<List<QuizAttemptDto>>> GetForQuiz(ISender sender, Guid quizId)
    {
        var result = await sender.Send(new GetQuizAttemptsByQuizQuery(quizId));
        return TypedResults.Ok(result);
    }

    public async Task<Ok<List<QuizAttemptDto>>> GetForStudent(ISender sender, Guid studentId)
    {
        var result = await sender.Send(new GetQuizAttemptsByStudentQuery(studentId));
        return TypedResults.Ok(result);
    }

    public async Task<Created<Guid>> StartAttempt(ISender sender, CreateQuizAttemptCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/{nameof(QuizAttempts)}/{id}", id);
    }

    public async Task<Results<NoContent, BadRequest>> CompleteAttempt(ISender sender, Guid attemptId, CompleteQuizAttemptCommand command)
    {
        if (attemptId != command.AttemptId) return TypedResults.BadRequest();
        await sender.Send(command);
        return TypedResults.NoContent();
    }
}
