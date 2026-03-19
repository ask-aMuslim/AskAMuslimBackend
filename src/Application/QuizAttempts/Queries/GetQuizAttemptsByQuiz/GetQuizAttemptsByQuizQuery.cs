using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.QuizAttempts.Queries.GetQuizAttemptsByQuiz;

public record GetQuizAttemptsByQuizQuery(Guid QuizId) : IRequest<Result<List<QuizAttemptDto>>>;

public class GetQuizAttemptsByQuizQueryHandler : IRequestHandler<GetQuizAttemptsByQuizQuery, Result<List<QuizAttemptDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetQuizAttemptsByQuizQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<QuizAttemptDto>>> Handle(GetQuizAttemptsByQuizQuery request, CancellationToken cancellationToken)
    {
        var attempts = await (from a in _context.QuizAttempts
                              where a.QuizId == request.QuizId
                              join s in _context.Students on a.StudentId equals s.Id into students
                              from s in students.DefaultIfEmpty()
                              join u in _context.Users on s != null ? s.UserId : Guid.Empty equals u.Id into users
                              from u in users.DefaultIfEmpty()
                              orderby a.StartedAt descending
                              select new QuizAttemptDto
                              {
                                  Id = a.Id,
                                  QuizId = a.QuizId,
                                  StudentId = a.StudentId,
                                  StudentName = u != null ? $"{u.FirstName} {u.LastName}".Trim() : string.Empty,
                                  Score = a.Score,
                                  IsPassed = a.IsPassed,
                                  StartedAt = a.StartedAt,
                                  CompletedAt = a.CompletedAt,
                                  AttemptNumber = a.AttemptNumber
                              })
                              .ToListAsync(cancellationToken);
        return Result<List<QuizAttemptDto>>.Success(attempts);
    }
}
