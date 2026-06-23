using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace AskAMuslimBackend.Application.QuizAttempts.Commands.CompleteQuizAttempt;

public record QuizAnswerDto
{
    public Guid QuestionId { get; init; }
    public Guid SelectedOptionId { get; init; }
}

public record CompleteQuizAttemptCommand : IRequest<Result>
{
    [JsonIgnore] // Set from route parameter, not request body
    public Guid AttemptId { get; init; }
    public List<QuizAnswerDto> Answers { get; init; } = new();
}

public class CompleteQuizAttemptCommandHandler : IRequestHandler<CompleteQuizAttemptCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public CompleteQuizAttemptCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(CompleteQuizAttemptCommand request, CancellationToken cancellationToken)
    {
        var attempt = await _context.QuizAttempts
            .Include(a => a.Quiz)
                .ThenInclude(q => q!.Questions)
                    .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(a => a.Id == request.AttemptId, cancellationToken);
            
        if (attempt == null)
        {
            return Result.Failure(new[] { "Quiz attempt not found." });
        }

        if (attempt.CompletedAt.HasValue)
        {
            return Result.Failure(new[] { "This quiz attempt has already been completed." });
        }

        if (attempt.Quiz == null)
        {
            return Result.Failure(new[] { "Quiz not found." });
        }

        // Validate that all questions are answered
        var quizQuestionIds = attempt.Quiz.Questions.Select(q => q.Id).ToHashSet();
        var answeredQuestionIds = request.Answers.Select(a => a.QuestionId).ToHashSet();
        
        if (!quizQuestionIds.SetEquals(answeredQuestionIds))
        {
            return Result.Failure(new[] { "All questions must be answered." });
        }

        // Calculate score based on correct answers
        decimal totalQuestions = attempt.Quiz.Questions.Count;
        decimal correctAnswers = 0;

        var quizAttemptAnswers = new List<Domain.Entities.QuizAttemptAnswer>();

        foreach (var answer in request.Answers)
        {
            var question = attempt.Quiz.Questions.FirstOrDefault(q => q.Id == answer.QuestionId);
            if (question == null)
            {
                return Result.Failure(new[] { $"Question {answer.QuestionId} not found in this quiz." });
            }

            var selectedOption = question.Options.FirstOrDefault(o => o.Id == answer.SelectedOptionId);
            if (selectedOption == null)
            {
                return Result.Failure(new[] { $"Option {answer.SelectedOptionId} not found for question {answer.QuestionId}." });
            }

            bool isCorrect = selectedOption.IsCorrect;
            if (isCorrect)
            {
                correctAnswers++;
            }

            quizAttemptAnswers.Add(new Domain.Entities.QuizAttemptAnswer
            {
                QuizAttemptId = attempt.Id,
                QuestionId = answer.QuestionId,
                SelectedOptionId = answer.SelectedOptionId,
                IsCorrect = isCorrect
            });
        }

        // Calculate percentage score
        decimal scorePercentage = totalQuestions > 0 ? (correctAnswers / totalQuestions) * 100 : 0;
        
        // Determine if passed (assuming 60% is passing grade - you can make this configurable)
        const decimal passingGrade = 60m;
        bool isPassed = scorePercentage >= passingGrade;

        // Save answers
        _context.QuizAttemptAnswers.AddRange(quizAttemptAnswers);

        // Update attempt
        attempt.Score = Math.Round(scorePercentage, 2);
        attempt.IsPassed = isPassed;
        attempt.CompletedAt = DateTime.UtcNow;

        // Save the attempt first so subsequent queries reflect the updated IsPassed flag
        await _context.SaveChangesAsync(cancellationToken);

        Guid? courseId = null;
        Guid? levelId = attempt.Quiz?.LevelId;

        // Resolve courseId from quiz target type: Course quiz or Lesson quiz
        if (attempt.Quiz?.CourseId is Guid cid)
        {
            courseId = cid;
        }
        else if (attempt.Quiz?.LessonId is Guid lid)
        {
            courseId = await _context.Lessons
                .Where(l => l.Id == lid)
                .Select(l => (Guid?)l.CourseId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        // If we have a course, update the enrollment's course progress
        if (courseId.HasValue)
        {
            // Find enrollment for this student & course
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.StudentId == attempt.StudentId && e.CourseId == courseId.Value, cancellationToken);

            if (enrollment != null)
            {
                // If this is a Course quiz and it has been passed, check if all lessons are already completed
                // so we can mark the course as Completed
                if (isPassed && attempt.Quiz?.TargetType == Domain.Enums.QuizTargetType.Course)
                {
                    var totalPublishedLessonsCount = await _context.Lessons
                        .AsNoTracking()
                        .CountAsync(l => l.CourseId == courseId.Value && l.IsPublished, cancellationToken);

                    var completedPublishedLessonsCount = await _context.StudentProgresses
                        .AsNoTracking()
                        .Where(sp => sp.StudentId == attempt.StudentId && sp.CourseId == courseId.Value && sp.IsCompleted)
                        .Join(
                            _context.Lessons.AsNoTracking().Where(l => l.CourseId == courseId.Value && l.IsPublished),
                            sp => sp.LessonId,
                            l => l.Id,
                            (sp, _) => sp.Id)
                        .CountAsync(cancellationToken);

                    if (completedPublishedLessonsCount >= totalPublishedLessonsCount && totalPublishedLessonsCount > 0)
                    {
                        if (enrollment.Status != Domain.Enums.EnrollmentStatus.Completed)
                        {
                            enrollment.Status = Domain.Enums.EnrollmentStatus.Completed;
                            await _context.SaveChangesAsync(cancellationToken);
                        }
                    }
                }

                // Resolve levelId (from quiz or from the course)
                levelId ??= await _context.Courses
                    .Where(c => c.Id == courseId.Value)
                    .Select(c => (Guid?)c.LevelId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (levelId.HasValue)
                {
                    // Level progress: percentage of courses in level that are completed by student
                    var levelCourseIds = await _context.Courses
                        .Where(c => c.LevelId == levelId.Value && c.IsPublished)
                        .Select(c => c.Id)
                        .ToListAsync(cancellationToken);

                    var completedCoursesCount = await _context.Enrollments
                        .Where(e => e.StudentId == attempt.StudentId && levelCourseIds.Contains(e.CourseId) && e.Status == Domain.Enums.EnrollmentStatus.Completed)
                        .Select(e => e.CourseId)
                        .Distinct()
                        .CountAsync(cancellationToken);

                    var totalLevelCourses = levelCourseIds.Count;
                    double levelProgress = 0d;
                    if (totalLevelCourses > 0)
                    {
                        levelProgress = completedCoursesCount * 100d / totalLevelCourses;
                    }

                    var studentLevelEnrollments = await _context.Enrollments
                        .Where(e => e.StudentId == attempt.StudentId && levelCourseIds.Contains(e.CourseId))
                        .ToListAsync(cancellationToken);

                    foreach (var e in studentLevelEnrollments)
                    {
                        e.LevelProgress = Math.Round(levelProgress, 2);
                    }
                }
            }
        }
        else if (levelId.HasValue)
        {
            // Level-only quiz: recompute LevelProgress for all enrollments of this student within the level
            var levelCourseIds = await _context.Courses
                .Where(c => c.LevelId == levelId.Value && c.IsPublished)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            var completedCoursesCount = await _context.Enrollments
                .Where(e => e.StudentId == attempt.StudentId && levelCourseIds.Contains(e.CourseId) && e.Status == Domain.Enums.EnrollmentStatus.Completed)
                .Select(e => e.CourseId)
                .Distinct()
                .CountAsync(cancellationToken);

            var totalLevelCourses = levelCourseIds.Count;
            double levelProgress = 0d;
            if (totalLevelCourses > 0)
            {
                levelProgress = completedCoursesCount * 100d / totalLevelCourses;
            }

            var studentLevelEnrollments = await _context.Enrollments
                .Where(e => e.StudentId == attempt.StudentId && levelCourseIds.Contains(e.CourseId))
                .ToListAsync(cancellationToken);

            foreach (var e in studentLevelEnrollments)
            {
                e.LevelProgress = Math.Round(levelProgress, 2);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public class CompleteQuizAttemptCommandValidator : AbstractValidator<CompleteQuizAttemptCommand>
{
    public CompleteQuizAttemptCommandValidator()
    {
        RuleFor(x => x.AttemptId)
            .NotEmpty().WithMessage("Attempt ID is required.");

        RuleFor(x => x.Answers)
            .NotNull().WithMessage("Answers are required.")
            .NotEmpty().WithMessage("At least one answer must be provided.");

        RuleForEach(x => x.Answers).ChildRules(answer =>
        {
            answer.RuleFor(a => a.QuestionId)
                .NotEmpty().WithMessage("Question ID is required.");
            
            answer.RuleFor(a => a.SelectedOptionId)
                .NotEmpty().WithMessage("Selected option ID is required.");
        });
    }
}
