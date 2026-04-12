using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.LessonFeedback.Queries.GetLessonFeedbackById;

public record GetLessonFeedbackByIdQuery(Guid Id) : IRequest<Result<LessonFeedbackDto?>>;

public class GetLessonFeedbackByIdQueryHandler : IRequestHandler<GetLessonFeedbackByIdQuery, Result<LessonFeedbackDto?>>
{
    private readonly IApplicationDbContext _context;

    public GetLessonFeedbackByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<LessonFeedbackDto?>> Handle(GetLessonFeedbackByIdQuery request, CancellationToken cancellationToken)
    {
        var feedback = await _context.LessonFeedbacks
            .AsNoTracking()
            .Where(f => f.Id == request.Id)
            .Select(f => new LessonFeedbackDto
            {
                Id = f.Id,
                StudentId = f.StudentId,
                LessonId = f.LessonId,
                Rating = f.Rating,
                Comment = f.Comment,
                Created = f.Created
            })
            .FirstOrDefaultAsync(cancellationToken);

        return Result<LessonFeedbackDto?>.Success(feedback);
    }
}
