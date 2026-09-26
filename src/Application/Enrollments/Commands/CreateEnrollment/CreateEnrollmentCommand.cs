using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Enrollments.Commands.CreateEnrollment;

public record CreateEnrollmentCommand : IRequest<Result<Guid>>
{
    public Guid StudentId { get; init; }
    public Guid CourseId { get; init; }
}

public class CreateEnrollmentCommandHandler : IRequestHandler<CreateEnrollmentCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateEnrollmentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateEnrollmentCommand request, CancellationToken cancellationToken)
    {
        var studentExists = await _context.Students.AnyAsync(s => s.Id == request.StudentId, cancellationToken);
        var course = await _context.Courses
            .Include(c => c.Level)
            .FirstOrDefaultAsync(c => c.Id == request.CourseId, cancellationToken);
            
        if (!studentExists || course == null)
        {
            var errors = new List<string>();
            if (!studentExists) errors.Add("Student does not exist.");
            if (course == null) errors.Add("Course does not exist.");
            return Result<Guid>.Failure(errors.ToArray());
        }

        var alreadyEnrolled = await _context.Enrollments
            .AsNoTracking()
            .AnyAsync(e => e.StudentId == request.StudentId && e.CourseId == request.CourseId, cancellationToken);

        if (alreadyEnrolled)
        {
            return Result<Guid>.Failure(new[] { "Student is already enrolled in this course." });
        }
        
        // Level Progression Check: Must pass previous level quiz if it exists
        if (course.Level != null)
        {
            var previousLevels = await _context.Levels
                .Where(l => l.Order < course.Level.Order && l.IsPublished)
                .OrderByDescending(l => l.Order)
                .ToListAsync(cancellationToken);
                
            foreach (var prevLevel in previousLevels)
            {
                // Find first previous level that requires a quiz
                if (prevLevel.HasQuiz)
                {
                    var passedLevelQuiz = await _context.QuizAttempts
                        .AnyAsync(a => a.StudentId == request.StudentId && 
                                       a.Quiz != null && 
                                       a.Quiz.LevelId == prevLevel.Id && 
                                       a.Quiz.TargetType == QuizTargetType.Level && 
                                       a.IsPassed, cancellationToken);

                    if (!passedLevelQuiz)
                    {
                        return Result<Guid>.Failure(new[] { $"You must pass the quiz for the previous level '{prevLevel.Title}' before enrolling in courses of level '{course.Level.Title}'." });
                    }
                    break; // Only enforce the immediately preceding level that has a quiz
                }
            }
        }

        // Check prerequisites - all must be completed
        var prerequisiteIds = await _context.CoursePrerequisites
            .Where(cp => cp.CourseId == request.CourseId && cp.PrerequisiteCourse != null && cp.PrerequisiteCourse.IsPublished)
            .Select(cp => cp.PrerequisiteCourseId)
            .ToListAsync(cancellationToken);

        if (prerequisiteIds.Any())
        {
            // Get completed course IDs for this student
            var completedCourseIds = await _context.Enrollments
                .Where(e => e.StudentId == request.StudentId && 
                           e.Status == EnrollmentStatus.Completed &&
                           prerequisiteIds.Contains(e.CourseId))
                .Select(e => e.CourseId)
                .ToListAsync(cancellationToken);

            // Find missing prerequisites
            var missingPrerequisites = prerequisiteIds.Except(completedCourseIds).ToList();

            if (missingPrerequisites.Any())
            {
                // Get course titles for better error message
                var missingCourseTitles = await _context.Courses
                    .Where(c => missingPrerequisites.Contains(c.Id))
                    .Select(c => c.Title)
                    .ToListAsync(cancellationToken);

                var errorMessage = $"You must complete the following prerequisite course(s) first: {string.Join(", ", missingCourseTitles)}.";
                return Result<Guid>.Failure(new[] { errorMessage });
            }
        }

        var totalLessonsCount = await _context.Lessons
            .CountAsync(l => l.CourseId == request.CourseId && l.IsPublished, cancellationToken);

        var entity = new Enrollment
        {
            StudentId = request.StudentId,
            CourseId = request.CourseId,
            EnrolledAt = DateTime.UtcNow,
            Status = EnrollmentStatus.Active,
            LevelProgress = 0,
            CompletedLessonsCount = 0,
            TotalLessonsCount = totalLessonsCount
        };

        _context.Enrollments.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(entity.Id);
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
