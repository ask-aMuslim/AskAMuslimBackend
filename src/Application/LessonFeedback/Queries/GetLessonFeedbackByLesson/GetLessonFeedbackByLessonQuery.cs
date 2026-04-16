using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.LessonFeedback.Queries.GetLessonFeedbackByLesson;

public record GetLessonFeedbackByLessonQuery(Guid LessonId) : IRequest<Result<List<LessonFeedbackDto>>>;

public class GetLessonFeedbackByLessonQueryHandler : IRequestHandler<GetLessonFeedbackByLessonQuery, Result<List<LessonFeedbackDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetLessonFeedbackByLessonQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<LessonFeedbackDto>>> Handle(GetLessonFeedbackByLessonQuery request, CancellationToken cancellationToken)
    {
        var feedback = await _context.LessonFeedbacks
            .AsNoTracking()
            .Where(f => f.LessonId == request.LessonId)
            .OrderByDescending(f => f.Created)
            .Select(f => new LessonFeedbackDto
            {
                Id = f.Id,
                StudentId = f.StudentId,
                LessonId = f.LessonId,
                Rating = f.Rating,
                Comment = f.Comment,
                Created = f.Created
            })
            .ToListAsync(cancellationToken);

        return Result<List<LessonFeedbackDto>>.Success(feedback);
    }
}
