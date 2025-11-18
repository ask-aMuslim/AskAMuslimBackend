using FluentValidation;
using Microsoft.EntityFrameworkCore;
using AskAMuslimBackend.Application.Common.Interfaces;

namespace AskAMuslimBackend.Application.Courses.Commands.CreateCourse;

public class CreateCourseCommandValidator : AbstractValidator<CreateCourseCommand>
{
    private readonly IApplicationDbContext _context;

    public CreateCourseCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.LevelId)
            .NotEmpty().WithMessage("Level is required.")
            .MustAsync(LevelExists).WithMessage("Level does not exist.");

        // Batch validate TagIds; list each missing tag explicitly.
        RuleFor(x => x.TagIds)
            .NotNull().WithMessage("TagIds must be provided (empty list allowed).")
            .CustomAsync(async (tagIds, context, ct) =>
            {
                if (tagIds == null || tagIds.Count == 0)
                    return;

                // Remove duplicates early for efficiency
                var distinct = tagIds.Distinct().ToList();

                var existing = await _context.Tags
                    .AsNoTracking()
                    .Where(t => distinct.Contains(t.Id))
                    .Select(t => t.Id)
                    .ToListAsync(ct);

                var missing = distinct.Except(existing).ToList();
                foreach (var id in missing)
                {
                    context.AddFailure("TagIds", $"Tag {id} does not exist.");
                }
            });

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.ThumbnailUrl)
            .MaximumLength(500).WithMessage("Thumbnail URL must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.ThumbnailUrl));

        RuleFor(x => x.Order)
            .GreaterThanOrEqualTo(0).WithMessage("Order must be 0 or greater.");
    }

    private Task<bool> LevelExists(Guid levelId, CancellationToken ct)
        => _context.Levels.AnyAsync(l => l.Id == levelId, ct);
}
