using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.LessonFeedback.Commands.CreateLessonFeedback;

public record CreateLessonFeedbackCommand : IRequest<Result<Guid>>
{
    public Guid LessonId { get; init; }
    public int Rating { get; init; }
    public string? Comment { get; init; }
}

public class CreateLessonFeedbackCommandHandler : IRequestHandler<CreateLessonFeedbackCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public CreateLessonFeedbackCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(CreateLessonFeedbackCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
        {
            return Result<Guid>.Failure(new[] { "Invalid user ID" });
        }

        var student = await _context.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        if (student == null)
        {
            return Result<Guid>.Failure(new[] { "Student not found" });
        }

        var lessonExists = await _context.Lessons
            .AsNoTracking()
            .AnyAsync(l => l.Id == request.LessonId, cancellationToken);

        if (!lessonExists)
        {
            return Result<Guid>.Failure(new[] { "Lesson does not exist." });
        }

        var existingFeedback = await _context.LessonFeedbacks
            .FirstOrDefaultAsync(f => f.StudentId == student.Id && f.LessonId == request.LessonId, cancellationToken);

        if (existingFeedback != null)
        {
            existingFeedback.Rating = request.Rating;
            existingFeedback.Comment = request.Comment?.Trim();
            await _context.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Success(existingFeedback.Id);
        }

        var feedback = new AskAMuslimBackend.Domain.Entities.LessonFeedback
        {
            StudentId = student.Id,
            LessonId = request.LessonId,
            Rating = request.Rating,
            Comment = request.Comment?.Trim()
        };

        _context.LessonFeedbacks.Add(feedback);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(feedback.Id);
    }
}

public class CreateLessonFeedbackCommandValidator : AbstractValidator<CreateLessonFeedbackCommand>
{
    public CreateLessonFeedbackCommandValidator()
    {
        RuleFor(x => x.LessonId).NotEmpty();
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Comment).MaximumLength(2000);
    }
}
