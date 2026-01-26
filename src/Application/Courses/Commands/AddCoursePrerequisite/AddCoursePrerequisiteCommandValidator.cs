using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.Courses.Commands.AddCoursePrerequisite;

public class AddCoursePrerequisiteCommandValidator : AbstractValidator<AddCoursePrerequisiteCommand>
{
    public AddCoursePrerequisiteCommandValidator()
    {
        RuleFor(v => v.CourseId)
            .NotEmpty()
            .WithMessage("Course ID is required.");

        RuleFor(v => v.PrerequisiteCourseId)
            .NotEmpty()
            .WithMessage("Prerequisite Course ID is required.");

        RuleFor(v => v.Order)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Order must be a non-negative integer.");
    }
}
