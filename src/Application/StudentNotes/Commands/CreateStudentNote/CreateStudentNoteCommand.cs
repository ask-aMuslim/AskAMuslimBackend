using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.StudentNotes.Commands.CreateStudentNote;

public record CreateStudentNoteCommand : IRequest<Result<Guid>>
{
    public Guid StudentId { get; init; }
    public Guid LessonId { get; init; }
    public string Text { get; init; } = string.Empty;
    public double Timestamp { get; init; }
}

public class CreateStudentNoteCommandHandler : IRequestHandler<CreateStudentNoteCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateStudentNoteCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateStudentNoteCommand request, CancellationToken cancellationToken)
    {
        var studentExists = await _context.Students.AnyAsync(s => s.Id == request.StudentId, cancellationToken);
        var lessonExists = await _context.Lessons.AnyAsync(l => l.Id == request.LessonId, cancellationToken);

        if (!studentExists || !lessonExists)
        {
            var errors = new List<string>();
            if (!studentExists) errors.Add("Student does not exist.");
            if (!lessonExists) errors.Add("Lesson does not exist.");
            return Result<Guid>.Failure(errors.ToArray());
        }

        var entity = new StudentNote
        {
            StudentId = request.StudentId,
            LessonId = request.LessonId,
            Text = request.Text,
            Timestamp = request.Timestamp
        };

        _context.StudentNotes.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(entity.Id);
    }
}

public class CreateStudentNoteCommandValidator : AbstractValidator<CreateStudentNoteCommand>
{
    public CreateStudentNoteCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.LessonId).NotEmpty();
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Text is required.")
            .MaximumLength(5000).WithMessage("Text must not exceed 5000 characters.");
        RuleFor(x => x.Timestamp).GreaterThanOrEqualTo(0);
    }
}
