using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Progress.Commands.CompleteLesson;

public record CompleteLessonCommand(Guid CourseId, Guid LessonId, double? VideoProgressPercentage) : IRequest<bool>;

public class CompleteLessonCommandHandler : IRequestHandler<CompleteLessonCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public CompleteLessonCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<bool> Handle(CompleteLessonCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_user.Id) || !Guid.TryParse(_user.Id, out var userId))
            throw new UnauthorizedAccessException();

        var student = await _context.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken)
            ?? throw new InvalidOperationException("Student profile not found for the current user.");

        var studentId = student.Id;

        var lesson = await _context.Lessons
            .FirstOrDefaultAsync(l => l.Id == request.LessonId && l.CourseId == request.CourseId && l.IsPublished, cancellationToken)
            ?? throw new InvalidOperationException("Lesson not found, is not published, or does not belong to the specified course.");

        // Prerequisite check: Must have completed the previous lesson's quiz if it has one
        var previousLesson = await _context.Lessons
            .Where(l => l.CourseId == request.CourseId && l.Order < lesson.Order && l.IsPublished)
            .OrderByDescending(l => l.Order)
            .FirstOrDefaultAsync(cancellationToken);

        if (previousLesson != null && previousLesson.HasQuiz)
        {
            bool passedPreviousQuiz = await _context.QuizAttempts
                .AnyAsync(a => a.StudentId == studentId && 
                               a.Quiz != null && 
                               a.Quiz.LessonId == previousLesson.Id && 
                               a.Quiz.TargetType == Domain.Enums.QuizTargetType.Lesson && 
                               a.IsPassed, cancellationToken);
            if (!passedPreviousQuiz)
            {
                throw new InvalidOperationException($"You must pass the quiz for the previous lesson '{previousLesson.Title}' before proceeding.");
            }
        }

        bool shouldComplete = true;
        if (request.VideoProgressPercentage.HasValue && request.VideoProgressPercentage < 90)
        {
            shouldComplete = false;
        }

        var studentProgress = await _context.StudentProgresses
            .FirstOrDefaultAsync(sp => sp.StudentId == studentId && sp.LessonId == request.LessonId, cancellationToken);

        if (studentProgress == null)
        {
            studentProgress = new StudentProgress
            {
                StudentId = studentId,
                CourseId = request.CourseId,
                LessonId = request.LessonId,
                IsCompleted = shouldComplete,
                ProgressPercentage = request.VideoProgressPercentage,
                LastAccessedAt = DateTime.UtcNow,
                CompletedAt = shouldComplete ? DateTime.UtcNow : null
            };
            _context.StudentProgresses.Add(studentProgress);
        }
        else
        {
            studentProgress.LastAccessedAt = DateTime.UtcNow;
            
            if (studentProgress.IsCompleted)
            {
                if (request.VideoProgressPercentage.HasValue && request.VideoProgressPercentage > studentProgress.ProgressPercentage)
                    studentProgress.ProgressPercentage = request.VideoProgressPercentage;
                
                await _context.SaveChangesAsync(cancellationToken);
                return false;
            }

            studentProgress.ProgressPercentage = request.VideoProgressPercentage ?? studentProgress.ProgressPercentage;
            
            if (shouldComplete)
            {
                studentProgress.IsCompleted = true;
                studentProgress.CompletedAt = DateTime.UtcNow;
            }
        }

        if (shouldComplete)
        {
            var enrollment = await _context.Enrollments
                .Include(e => e.Course)
                    .ThenInclude(c => c!.Lessons)
                .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == request.CourseId, cancellationToken);

            if (enrollment == null)
            {
                throw new InvalidOperationException("Student is not enrolled in this course.");
            }

            enrollment.TotalLessonsCount = enrollment.Course!.Lessons.Count(l => l.IsPublished);

            // Count completed published lessons excluding the current lesson (which we just completed in-memory)
            var currentCompletedCount = await _context.StudentProgresses
                .AsNoTracking()
                .Where(sp => sp.StudentId == studentId && sp.CourseId == request.CourseId && sp.IsCompleted && sp.LessonId != request.LessonId)
                .Join(
                    _context.Lessons.AsNoTracking().Where(l => l.CourseId == request.CourseId && l.IsPublished),
                    sp => sp.LessonId,
                    l => l.Id,
                    (sp, _) => sp.Id)
                .CountAsync(cancellationToken);

            enrollment.CompletedLessonsCount = currentCompletedCount + 1;
            enrollment.LastUpdatedAt = DateTime.UtcNow;

            // Mark course as completed if all lessons are done AND (course has no quiz OR course quiz is passed)
            if (enrollment.CompletedLessonsCount >= enrollment.TotalLessonsCount)
            {
                if (!enrollment.Course!.HasQuiz)
                {
                    enrollment.Status = AskAMuslimBackend.Domain.Enums.EnrollmentStatus.Completed;
                }
                else
                {
                    // Course has a quiz, verify if the user has passed the course quiz
                    bool hasPassedCourseQuiz = await _context.QuizAttempts
                        .AnyAsync(a => a.StudentId == studentId && a.Quiz != null && 
                                       a.Quiz.CourseId == request.CourseId && 
                                       a.Quiz.TargetType == Domain.Enums.QuizTargetType.Course && 
                                       a.IsPassed, cancellationToken);
                                       
                    if (hasPassedCourseQuiz)
                    {
                        enrollment.Status = AskAMuslimBackend.Domain.Enums.EnrollmentStatus.Completed;
                    }
                }
            }
        }
        
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
