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

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required.")
            .MustAsync(CategoryExists).WithMessage("Category does not exist.");

        RuleFor(x => x.LevelId)
            .NotEmpty().WithMessage("Level is required.")
            .MustAsync(LevelExists).WithMessage("Level does not exist.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.ThumbnailUrl)
            .MaximumLength(500).WithMessage("Thumbnail URL must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.ThumbnailUrl));

        RuleFor(x => x.Order)
            .GreaterThanOrEqualTo(0).WithMessage("Order must be 0 or greater.");
    }

    private async Task<bool> CategoryExists(Guid categoryId, CancellationToken ct)
        => await _context.Categories.AnyAsync(c => c.Id == categoryId, ct);

    private async Task<bool> LevelExists(Guid levelId, CancellationToken ct)
        => await _context.Levels.AnyAsync(l => l.Id == levelId, ct);
}
