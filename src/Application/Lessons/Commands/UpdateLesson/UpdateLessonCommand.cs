using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Validators;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;

namespace AskAMuslimBackend.Application.Lessons.Commands.UpdateLesson;

public record UpdateLessonCommand : IRequest<Result>
{
    public Guid Id { get; init; }
    public Guid CourseId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ContentJson { get; init; }
    public LessonType Type { get; init; } = LessonType.Video;
    public string? ContentUrl { get; init; }
    public string? Transcript { get; init; }
    public string? ThumbnailUrl { get; init; }
    public int Order { get; init; }
    public bool IsPublished { get; init; }
}

public class UpdateLessonCommandHandler : IRequestHandler<UpdateLessonCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateLessonCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UpdateLessonCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Lessons.FindAsync(new object[] { request.Id }, cancellationToken);
        
        if (entity == null)
            return Result.Failure(new[] { "Lesson not found." });

        // Optional: ensure course relation not broken
        if (entity.CourseId != request.CourseId)
        {
            // Ensure target course exists
            var course = await _context.Courses.FindAsync(new object[] { request.CourseId }, cancellationToken);
            if (course == null)
                return Result.Failure(new[] { "Course not found." });
            entity.CourseId = request.CourseId;
        }

        entity.Title = request.Title;
        entity.Description = request.Description;
        entity.ContentJson = request.ContentJson;
        entity.Type = request.Type;
        entity.ContentUrl = request.ContentUrl;
        entity.Transcript = request.Transcript;
        entity.ThumbnailUrl = request.ThumbnailUrl;
        entity.Order = request.Order;
        entity.IsPublished = request.IsPublished;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public class UpdateLessonCommandValidator : AbstractValidator<UpdateLessonCommand>
{
    public UpdateLessonCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => !string.IsNullOrEmpty(x.Description));
        RuleFor(x => x.ContentUrl).MaximumLength(500).When(x => !string.IsNullOrEmpty(x.ContentUrl));
        RuleFor(x => x.ThumbnailUrl).MaximumLength(500).When(x => !string.IsNullOrEmpty(x.ThumbnailUrl));
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Order).GreaterThanOrEqualTo(0);

        RuleFor(x => x.ContentJson)
            .Must(BeValidTiptapJson).WithMessage(x => GetTiptapJsonError(x.ContentJson))
            .When(x => !string.IsNullOrEmpty(x.ContentJson));
    }

    private bool BeValidTiptapJson(string? json)
        => TiptapJsonValidator.IsValidTiptapJson(json, out _);

    private string GetTiptapJsonError(string? json)
    {
        TiptapJsonValidator.IsValidTiptapJson(json, out var error);
        return error ?? "Invalid Tiptap JSON.";
    }
}
