using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Progress.Commands.CompleteLesson;

public record CompleteLessonCommand(Guid CourseId, Guid LessonId, double? ProgressPercentage, bool MarkAsRead = false) : IRequest<CompleteLessonResult>;

public enum CompletionStatus
{
    AlreadyCompleted,
    Completed,
    ProgressUpdated,
    QuizRequired
}

public record CompleteLessonResult(CompletionStatus Status, double? CurrentProgressPercentage, string? Message = null);

public class CompleteLessonCommandHandler : IRequestHandler<CompleteLessonCommand, CompleteLessonResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public CompleteLessonCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<CompleteLessonResult> Handle(CompleteLessonCommand request, CancellationToken cancellationToken)
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

        // Determine completion based on lesson type, an explicit mark, or reported progress
        var progress = request.ProgressPercentage;
        var markAsRead = request.MarkAsRead;
        bool shouldComplete = false;

        if (markAsRead)
        {
            shouldComplete = true;
        }
        else if (progress.HasValue)
        {
            switch (lesson.Type)
            {
                case LessonType.Video:
                case LessonType.Audio:
                    shouldComplete = progress.Value >= 90;
                    break;
                case LessonType.Article:
                case LessonType.Document:
                    shouldComplete = progress.Value >= 80;
                    break;
                default:
                    shouldComplete = progress.Value >= 90;
                    break;
            }
        }
        else
        {
            // Backwards-compatible behavior: if neither progress nor explicit mark provided, treat as completed
            shouldComplete = true;
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
                ProgressPercentage = request.ProgressPercentage,
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
                if (request.ProgressPercentage.HasValue && request.ProgressPercentage > studentProgress.ProgressPercentage)
                    studentProgress.ProgressPercentage = request.ProgressPercentage;

                await _context.SaveChangesAsync(cancellationToken);
                return new CompleteLessonResult(CompletionStatus.AlreadyCompleted, studentProgress.ProgressPercentage);
            }

            studentProgress.ProgressPercentage = request.ProgressPercentage ?? studentProgress.ProgressPercentage;

            if (shouldComplete)
            {
                studentProgress.IsCompleted = true;
                studentProgress.CompletedAt = DateTime.UtcNow;
            }
        }

        Enrollment? enrollment = null;

        if (shouldComplete)
        {
            enrollment = await _context.Enrollments
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

        var status = shouldComplete
            ? CompletionStatus.Completed
            : CompletionStatus.ProgressUpdated;

        return new CompleteLessonResult(
            status,
            studentProgress?.ProgressPercentage,
            status == CompletionStatus.Completed && enrollment?.Status == EnrollmentStatus.Completed
                ? "Course completed!"
                : null);
    }
}
