using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.Courses.Commands.RemoveCoursePrerequisite;

public class RemoveCoursePrerequisiteCommandValidator : AbstractValidator<RemoveCoursePrerequisiteCommand>
{
    public RemoveCoursePrerequisiteCommandValidator()
    {
        RuleFor(v => v.CourseId)
            .NotEmpty()
            .WithMessage("Course ID is required.");

        RuleFor(v => v.PrerequisiteCourseId)
            .NotEmpty()
            .WithMessage("Prerequisite Course ID is required.");
    }
}
