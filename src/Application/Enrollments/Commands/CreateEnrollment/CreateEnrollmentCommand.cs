using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Enrollments.Commands.CreateEnrollment;

public record CreateEnrollmentCommand : IRequest<Guid>
{
    public Guid StudentId { get; init; }
    public Guid CourseId { get; init; }
}

public class CreateEnrollmentCommandHandler : IRequestHandler<CreateEnrollmentCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateEnrollmentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateEnrollmentCommand request, CancellationToken cancellationToken)
    {
        var studentExists = await _context.Students.AnyAsync(s => s.Id == request.StudentId, cancellationToken);
        var courseExists = await _context.Courses.AnyAsync(c => c.Id == request.CourseId, cancellationToken);

        if (!studentExists || !courseExists)
        {
            var failures = new List<FluentValidation.Results.ValidationFailure>();
            if (!studentExists) failures.Add(new("StudentId", "Student does not exist."));
            if (!courseExists) failures.Add(new("CourseId", "Course does not exist."));
            throw new ValidationException(failures);
        }

        var alreadyEnrolled = await _context.Enrollments
            .AsNoTracking()
            .AnyAsync(e => e.StudentId == request.StudentId && e.CourseId == request.CourseId, cancellationToken);

        if (alreadyEnrolled)
        {
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("CourseId", "Student is already enrolled in this course.") });
        }

        var entity = new Enrollment
        {
            StudentId = request.StudentId,
            CourseId = request.CourseId,
            EnrolledAt = DateTime.UtcNow,
            Status = EnrollmentStatus.Active,
            LevelProgress = 0,
            CourseProgress = 0
        };

        _context.Enrollments.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

public class CreateEnrollmentCommandValidator : AbstractValidator<CreateEnrollmentCommand>
{
    public CreateEnrollmentCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.CourseId).NotEmpty();
    }
}
