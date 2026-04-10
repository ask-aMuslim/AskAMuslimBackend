using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;
using System.Text.Json.Serialization;

namespace AskAMuslimBackend.Application.Quizzes.Commands.UpdateQuiz;

public record UpdateQuizCommand : IRequest<Result>
{
    [JsonIgnore] // Set from route parameter, not request body
    public Guid Id { get; init; }
    public Guid? LevelId { get; init; }
    public Guid? CourseId { get; init; }
    public Guid? LessonId { get; init; }
    public QuizTargetType TargetType { get; init; }
    public string Title { get; init; } = string.Empty;
    public double TotalMarks { get; init; }
}

public class UpdateQuizCommandHandler : IRequestHandler<UpdateQuizCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ILocalizationService _localizer;

    public UpdateQuizCommandHandler(IApplicationDbContext context, ILocalizationService localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    public async Task<Result> Handle(UpdateQuizCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Quizzes.FindAsync(new object[] { request.Id }, cancellationToken);
        if (entity == null)
        {
            return Result.Failure(new[] { _localizer["Quizzes.NotFound"] });
        }

        // Keep track of old target to update its HasQuiz flag if target changes
        var oldTargetType = entity.TargetType;
        var oldLevelId = entity.LevelId;
        var oldCourseId = entity.CourseId;
        var oldLessonId = entity.LessonId;

        // Validate target exists if changed, and set HasQuiz on new target
        bool targetExists = false;
        switch (request.TargetType)
        {
            case QuizTargetType.Level:
                if (request.LevelId.HasValue)
                {
                    var level = await _context.Levels.FirstOrDefaultAsync(l => l.Id == request.LevelId.Value, cancellationToken);
                    if (level != null)
                    {
                        level.HasQuiz = true;
                        targetExists = true;
                    }
                }
                break;
            case QuizTargetType.Course:
                if (request.CourseId.HasValue)
                {
                    var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == request.CourseId.Value, cancellationToken);
                    if (course != null)
                    {
                        course.HasQuiz = true;
                        targetExists = true;
                    }
                }
                break;
            case QuizTargetType.Lesson:
                if (request.LessonId.HasValue)
                {
                    var lesson = await _context.Lessons.FirstOrDefaultAsync(l => l.Id == request.LessonId.Value, cancellationToken);
                    if (lesson != null)
                    {
                        lesson.HasQuiz = true;
                        targetExists = true;
                    }
                }
                break;
        }

        if (!targetExists)
        {
            return Result.Failure(new[] { _localizer["Quizzes.TargetDoesNotExist"] });
        }

        entity.LevelId = request.LevelId;
        entity.CourseId = request.CourseId;
        entity.LessonId = request.LessonId;
        entity.TargetType = request.TargetType;
        entity.Title = request.Title;
        entity.TotalMarks = request.TotalMarks;

        await _context.SaveChangesAsync(cancellationToken);

        // Re-evaluate old target to revert HasQuiz if no other quizzes exist
        if ((oldTargetType != request.TargetType) ||
            (oldTargetType == QuizTargetType.Level && oldLevelId != request.LevelId) ||
            (oldTargetType == QuizTargetType.Course && oldCourseId != request.CourseId) ||
            (oldTargetType == QuizTargetType.Lesson && oldLessonId != request.LessonId))
        {
            if (oldTargetType == QuizTargetType.Course && oldCourseId.HasValue)
            {
                bool hasMoreQuizzes = await _context.Quizzes.AnyAsync(q => q.TargetType == QuizTargetType.Course && q.CourseId == oldCourseId.Value && q.Id != entity.Id, cancellationToken);
                if (!hasMoreQuizzes)
                {
                    var course = await _context.Courses.FindAsync(new object[] { oldCourseId.Value }, cancellationToken);
                    if (course != null)
                    {
                        course.HasQuiz = false;
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                }
            }
            else if (oldTargetType == QuizTargetType.Lesson && oldLessonId.HasValue)
            {
                bool hasMoreQuizzes = await _context.Quizzes.AnyAsync(q => q.TargetType == QuizTargetType.Lesson && q.LessonId == oldLessonId.Value && q.Id != entity.Id, cancellationToken);
                if (!hasMoreQuizzes)
                {
                    var lesson = await _context.Lessons.FindAsync(new object[] { oldLessonId.Value }, cancellationToken);
                    if (lesson != null)
                    {
                        lesson.HasQuiz = false;
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                }
            }
            else if (oldTargetType == QuizTargetType.Level && oldLevelId.HasValue)
            {
                bool hasMoreQuizzes = await _context.Quizzes.AnyAsync(q => q.TargetType == QuizTargetType.Level && q.LevelId == oldLevelId.Value && q.Id != entity.Id, cancellationToken);
                if (!hasMoreQuizzes)
                {
                    var level = await _context.Levels.FindAsync(new object[] { oldLevelId.Value }, cancellationToken);
                    if (level != null)
                    {
                        level.HasQuiz = false;
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                }
            }
        }

        return Result.Success();
    }
}

public class UpdateQuizCommandValidator : AbstractValidator<UpdateQuizCommand>
{
    public UpdateQuizCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.TargetType).IsInEnum();
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");
        RuleFor(x => x.TotalMarks)
            .GreaterThanOrEqualTo(0).WithMessage("TotalMarks must be 0 or greater.");

        When(x => x.TargetType == QuizTargetType.Level, () =>
        {
            RuleFor(x => x.LevelId).NotEmpty().WithMessage("LevelId is required for a level quiz.");
        });
        When(x => x.TargetType == QuizTargetType.Course, () =>
        {
            RuleFor(x => x.CourseId).NotEmpty().WithMessage("CourseId is required for a course quiz.");
        });
        When(x => x.TargetType == QuizTargetType.Lesson, () =>
        {
            RuleFor(x => x.LessonId).NotEmpty().WithMessage("LessonId is required for a lesson quiz.");
        });
    }
}
