using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.StudentQuestions.Commands.CreateStudentQuestion;

public record CreateStudentQuestionCommand : IRequest<Guid>
{
    public Guid StudentId { get; init; }
    public Guid LessonId { get; init; }
    public string QuestionText { get; init; } = string.Empty;
    public double Timestamp { get; init; }
}

public class CreateStudentQuestionCommandHandler : IRequestHandler<CreateStudentQuestionCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateStudentQuestionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateStudentQuestionCommand request, CancellationToken cancellationToken)
    {
        var studentExists = await _context.Students.AnyAsync(s => s.Id == request.StudentId, cancellationToken);
        var lessonExists = await _context.Lessons.AnyAsync(l => l.Id == request.LessonId, cancellationToken);

        if (!studentExists || !lessonExists)
        {
            var failures = new List<FluentValidation.Results.ValidationFailure>();
            if (!studentExists) failures.Add(new("StudentId", "Student does not exist."));
            if (!lessonExists) failures.Add(new("LessonId", "Lesson does not exist."));
            throw new ValidationException(failures);
        }

        var entity = new StudentQuestion
        {
            StudentId = request.StudentId,
            LessonId = request.LessonId,
            QuestionText = request.QuestionText,
            Timestamp = request.Timestamp,
            Answered = false
        };

        _context.StudentQuestions.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

public class CreateStudentQuestionCommandValidator : AbstractValidator<CreateStudentQuestionCommand>
{
    public CreateStudentQuestionCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.LessonId).NotEmpty();
        RuleFor(x => x.QuestionText)
            .NotEmpty().WithMessage("QuestionText is required.")
            .MaximumLength(2000).WithMessage("QuestionText must not exceed 2000 characters.");
        RuleFor(x => x.Timestamp).GreaterThanOrEqualTo(0);
    }
}
