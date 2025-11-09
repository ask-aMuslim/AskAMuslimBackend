using FluentValidation;

namespace AskAMuslimBackend.Application.Courses.Commands.DeleteCourse;

public class DeleteCourseCommandValidator : AbstractValidator<DeleteCourseCommand>
{
    public DeleteCourseCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Course Id is required.");
    }
}
