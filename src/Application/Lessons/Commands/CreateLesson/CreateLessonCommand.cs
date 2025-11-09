using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Lessons.Commands.CreateLesson;

public record CreateLessonCommand : IRequest<Guid>
{
    public Guid CourseId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public LessonType Type { get; init; } = LessonType.Video;
    public string? ContentUrl { get; init; }
    public string? Transcript { get; init; }
    public string? ThumbnailUrl { get; init; }
    public int Order { get; init; }
}

public class CreateLessonCommandHandler : IRequestHandler<CreateLessonCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateLessonCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateLessonCommand request, CancellationToken cancellationToken)
    {
        // Defensive existence checks (in addition to FluentValidation) to avoid FK violation surfacing as DbUpdateException.
        var courseExists = await _context.Courses.AsNoTracking().AnyAsync(c => c.Id == request.CourseId, cancellationToken);
        if (!courseExists)
        {
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("CourseId", "Course does not exist.") });
        }

        var lesson = new Lesson
        {
            CourseId = request.CourseId,
            Title = request.Title,
            Description = request.Description,
            Type = request.Type,
            ContentUrl = request.ContentUrl,
            Transcript = request.Transcript,
            ThumbnailUrl = request.ThumbnailUrl,
            Order = request.Order
        };

        _context.Lessons.Add(lesson);
        await _context.SaveChangesAsync(cancellationToken);
        return lesson.Id;
    }
}

public class CreateLessonCommandValidator : AbstractValidator<CreateLessonCommand>
{
    private readonly IApplicationDbContext _context;

    public CreateLessonCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.CourseId)
            .NotEmpty().WithMessage("Course is required.")
            .MustAsync(CourseExists).WithMessage("Course does not exist.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.ContentUrl)
            .MaximumLength(500).WithMessage("Content URL must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.ContentUrl));

        RuleFor(x => x.ThumbnailUrl)
            .MaximumLength(500).WithMessage("Thumbnail URL must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.ThumbnailUrl));

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid lesson type.");

        RuleFor(x => x.Order)
            .GreaterThanOrEqualTo(0).WithMessage("Order must be 0 or greater.");
    }

    private async Task<bool> CourseExists(Guid courseId, CancellationToken ct)
        => await _context.Courses.AnyAsync(c => c.Id == courseId, ct);
}
