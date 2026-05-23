using AskAMuslimBackend.Application.QuizAttempts.Commands.CompleteQuizAttempt;
using AskAMuslimBackend.Application.QuizAttempts.Commands.CreateQuizAttempt;
using AskAMuslimBackend.Application.QuizAttempts.Queries;
using AskAMuslimBackend.Application.QuizAttempts.Queries.GetQuizAttemptById;
using AskAMuslimBackend.Application.QuizAttempts.Queries.GetQuizAttemptsByQuiz;
using AskAMuslimBackend.Application.QuizAttempts.Queries.GetQuizAttemptsByStudent;
using AskAMuslimBackend.Web.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AskAMuslimBackend.Web.Endpoints;

public class QuizAttempts : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetForQuiz, "by-quiz/{quizId}").WithName("GetQuizAttemptsByQuiz").WithSummary("Get quiz attempts for a quiz");
        groupBuilder.MapGet(GetForStudent, "by-student/{studentId}")
            .RequireAuthorization()
            .WithName("GetQuizAttemptsByStudent")
            .WithSummary("Get quiz attempts for a student (own attempts or admin)");
        groupBuilder.MapGet(GetById, "{id}")
            .RequireAuthorization()
            .WithName("GetQuizAttemptById")
            .WithSummary("Get a quiz attempt with questions, answers, and corrections");
        groupBuilder.MapPost(StartAttempt)
            .RequireAuthorization()
            .WithName("StartQuizAttempt")
            .WithSummary("Start a new quiz attempt");
        groupBuilder.MapPut(CompleteAttempt, "complete/{attemptId}")
            .RequireAuthorization()
            .WithName("CompleteQuizAttempt")
            .WithSummary("Complete a quiz attempt");
    }

    public async Task<IResult> GetForQuiz(ISender sender, Guid quizId)
    {
        var result = await sender.Send(new GetQuizAttemptsByQuizQuery(quizId));
        return result.ToHttpResult();
    }

    public async Task<IResult> GetForStudent(ISender sender, Guid studentId)
    {
        var result = await sender.Send(new GetQuizAttemptsByStudentQuery(studentId));
        return result.ToHttpResult();
    }

    public async Task<IResult> GetById(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetQuizAttemptByIdQuery(id));
        return result.ToHttpResult();
    }

    public async Task<IResult> StartAttempt(ISender sender, CreateQuizAttemptCommand command)
    {
        var result = await sender.Send(command);
        return result.ToCreatedResult($"/{nameof(QuizAttempts)}/{result.Data}");
    }

    public async Task<IResult> CompleteAttempt(ISender sender, Guid attemptId, CompleteQuizAttemptCommand command)
    {
        var result = await sender.Send(command with { AttemptId = attemptId });
        return result.ToHttpResult();
    }
}
