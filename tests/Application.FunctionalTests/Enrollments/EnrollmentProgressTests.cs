using AskAMuslimBackend.Application.Enrollments.Commands.CreateEnrollment;
using AskAMuslimBackend.Application.Levels.Commands.CreateLevel;
using AskAMuslimBackend.Application.Courses.Commands.CreateCourse;
using AskAMuslimBackend.Application.Lessons.Commands.CreateLesson;
using AskAMuslimBackend.Application.Progress.Commands.CompleteLesson;
using AskAMuslimBackend.Application.Quizzes.Commands.CreateQuiz;
using AskAMuslimBackend.Application.QuizAttempts.Commands.CreateQuizAttempt;
using AskAMuslimBackend.Application.QuizAttempts.Commands.CompleteQuizAttempt;
using AskAMuslimBackend.Application.Questions.Commands.CreateQuestion;
using AskAMuslimBackend.Application.Options.Commands.CreateOption;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.FunctionalTests.Enrollments;

using static Testing;

/// <summary>
/// Course progress is lesson-based: CompletedLessonsCount / TotalLessonsCount of published lessons.
/// A course with a course quiz only completes once that quiz is passed; level progress is recomputed on quiz completion.
/// </summary>
public class EnrollmentProgressTests : BaseTestFixture
{
    private readonly Dictionary<Guid, (Guid questionId, Guid correctOptionId, Guid incorrectOptionId)> _quizQuestions = new();

    private async Task<Guid> CreateStudentAsync()
    {
        // Students.UserId has a foreign key to Users, so create a real user first.
        // The student stays the current user, which CompleteLessonCommand resolves the student from.
        var userId = Guid.Parse(await RunAsUserAsync("student@local", "Testing1234!", UserRole.Student));
        var student = new Student { UserId = userId };
        await AddAsync(student);
        return student.Id;
    }

    private async Task<Guid> CreateLevelAsync()
    {
        var levelResult = await SendAsync(new CreateLevelCommand
        {
            Title = "Level 1",
            Description = "Level description",
            Difficulty = LevelDifficulty.Beginner,
            Order = 1,
            IsPublished = true
        });
        return levelResult.Data;
    }

    private async Task<Guid> CreateCourseAsync(Guid levelId, string title, int order)
    {
        var courseResult = await SendAsync(new CreateCourseCommand
        {
            LevelId = levelId,
            Title = title,
            Description = "Course description",
            IsPublished = true,
            Order = order,
            TagIds = new()
        });
        return courseResult.Data;
    }

    private async Task<Guid> CreateLessonAsync(Guid courseId, string title, int order)
    {
        var lessonResult = await SendAsync(new CreateLessonCommand
        {
            CourseId = courseId,
            Title = title,
            Order = order,
            IsPublished = true
        });
        return lessonResult.Data;
    }

    private async Task<(Guid levelId, Guid courseId, Guid lesson1Id, Guid lesson2Id)> CreateContentGraphAsync()
    {
        var levelId = await CreateLevelAsync();
        var courseId = await CreateCourseAsync(levelId, "Course 1", 1);
        var lesson1Id = await CreateLessonAsync(courseId, "Lesson 1", 1);
        var lesson2Id = await CreateLessonAsync(courseId, "Lesson 2", 2);

        return (levelId, courseId, lesson1Id, lesson2Id);
    }

    private async Task<Guid> CreateQuizAsync(QuizTargetType targetType, Guid? levelId = null, Guid? courseId = null, Guid? lessonId = null)
    {
        var result = await SendAsync(new CreateQuizCommand
        {
            TargetType = targetType,
            LevelId = levelId,
            CourseId = courseId,
            LessonId = lessonId,
            Title = $"Quiz - {targetType}",
            TotalMarks = 100
        });
        var quizId = result.Data;

        // Add a question with correct and incorrect options
        var questionResult = await SendAsync(new CreateQuestionCommand
        {
            QuizId = quizId,
            Text = "Test Question"
        });
        var questionId = questionResult.Data;

        var correctOptionResult = await SendAsync(new CreateOptionCommand
        {
            QuestionId = questionId,
            Text = "Correct Answer",
            IsCorrect = true
        });

        var incorrectOptionResult = await SendAsync(new CreateOptionCommand
        {
            QuestionId = questionId,
            Text = "Incorrect Answer",
            IsCorrect = false
        });

        _quizQuestions[quizId] = (questionId, correctOptionResult.Data, incorrectOptionResult.Data);

        return quizId;
    }

    private async Task<Guid> EnrollAsync(Guid studentId, Guid courseId)
    {
        var result = await SendAsync(new CreateEnrollmentCommand
        {
            StudentId = studentId,
            CourseId = courseId
        });
        result.Succeeded.ShouldBeTrue(string.Join(", ", result.Errors));
        return result.Data;
    }

    private async Task TakeQuizAsync(Guid quizId, Guid studentId, bool pass)
    {
        var attempt = await SendAsync(new CreateQuizAttemptCommand
        {
            QuizId = quizId,
            StudentId = studentId
        });

        var (questionId, correctOptionId, incorrectOptionId) = _quizQuestions[quizId];

        await SendAsync(new CompleteQuizAttemptCommand
        {
            AttemptId = attempt.Data,
            Answers = new List<QuizAnswerDto>
            {
                new QuizAnswerDto
                {
                    QuestionId = questionId,
                    SelectedOptionId = pass ? correctOptionId : incorrectOptionId
                }
            }
        });
    }

    private static Task<CompleteLessonResult> CompleteLessonAsync(Guid courseId, Guid lessonId) =>
        SendAsync(new CompleteLessonCommand(courseId, lessonId, ProgressPercentage: null, MarkAsRead: true));

    [Test]
    public async Task Should_Update_CourseProgress_As_Lessons_Are_Completed()
    {
        var studentId = await CreateStudentAsync();
        var (_, courseId, lesson1Id, lesson2Id) = await CreateContentGraphAsync();

        var enrollmentId = await EnrollAsync(studentId, courseId);

        var enrollment = await FindAsync<Enrollment>(enrollmentId);
        enrollment!.TotalLessonsCount.ShouldBe(2);
        enrollment.CourseProgress.ShouldBe(0);

        // Complete lesson 1 => 1/2 = 50%
        (await CompleteLessonAsync(courseId, lesson1Id)).Status.ShouldBe(CompletionStatus.Completed);

        enrollment = await FindAsync<Enrollment>(enrollmentId);
        enrollment!.CourseProgress.ShouldBe(50);
        enrollment.Status.ShouldBe(EnrollmentStatus.Active);

        // Completing the same lesson again => progress unchanged
        (await CompleteLessonAsync(courseId, lesson1Id)).Status.ShouldBe(CompletionStatus.AlreadyCompleted);

        enrollment = await FindAsync<Enrollment>(enrollmentId);
        enrollment!.CourseProgress.ShouldBe(50);

        // Complete lesson 2 => 100% and the course (no course quiz) is completed
        var result = await CompleteLessonAsync(courseId, lesson2Id);
        result.Message.ShouldBe("Course completed!");

        enrollment = await FindAsync<Enrollment>(enrollmentId);
        enrollment!.CourseProgress.ShouldBe(100);
        enrollment.Status.ShouldBe(EnrollmentStatus.Completed);
    }

    [Test]
    public async Task Should_Require_Passing_Previous_Lesson_Quiz_Before_Next_Lesson()
    {
        var studentId = await CreateStudentAsync();
        var (_, courseId, lesson1Id, lesson2Id) = await CreateContentGraphAsync();
        var lessonQuiz1Id = await CreateQuizAsync(QuizTargetType.Lesson, lessonId: lesson1Id);

        var enrollmentId = await EnrollAsync(studentId, courseId);

        await CompleteLessonAsync(courseId, lesson1Id);

        // Lesson 1 has a quiz that hasn't been passed yet
        await Should.ThrowAsync<InvalidOperationException>(() => CompleteLessonAsync(courseId, lesson2Id));

        // Failing the quiz doesn't unlock lesson 2
        await TakeQuizAsync(lessonQuiz1Id, studentId, pass: false);
        await Should.ThrowAsync<InvalidOperationException>(() => CompleteLessonAsync(courseId, lesson2Id));

        // Passing it does
        await TakeQuizAsync(lessonQuiz1Id, studentId, pass: true);
        await CompleteLessonAsync(courseId, lesson2Id);

        var enrollment = await FindAsync<Enrollment>(enrollmentId);
        enrollment!.CourseProgress.ShouldBe(100);
        enrollment.Status.ShouldBe(EnrollmentStatus.Completed);
    }

    [Test]
    public async Task Should_Complete_Course_After_Course_Quiz_And_Update_LevelProgress()
    {
        var studentId = await CreateStudentAsync();
        var (levelId, courseId, lesson1Id, lesson2Id) = await CreateContentGraphAsync();

        // A second published course in the same level that the student won't complete
        var otherCourseId = await CreateCourseAsync(levelId, "Course 2", 2);
        await CreateLessonAsync(otherCourseId, "Other Lesson", 1);

        var courseQuizId = await CreateQuizAsync(QuizTargetType.Course, courseId: courseId);

        var enrollmentId = await EnrollAsync(studentId, courseId);
        var otherEnrollmentId = await EnrollAsync(studentId, otherCourseId);

        // All lessons done => 100% progress, but the course quiz is still required
        await CompleteLessonAsync(courseId, lesson1Id);
        await CompleteLessonAsync(courseId, lesson2Id);

        var enrollment = await FindAsync<Enrollment>(enrollmentId);
        enrollment!.CourseProgress.ShouldBe(100);
        enrollment.Status.ShouldBe(EnrollmentStatus.Active);

        // Failing the course quiz keeps the course active
        await TakeQuizAsync(courseQuizId, studentId, pass: false);

        enrollment = await FindAsync<Enrollment>(enrollmentId);
        enrollment!.Status.ShouldBe(EnrollmentStatus.Active);
        enrollment.LevelProgress.ShouldBe(0);

        // Passing it completes the course => 1 of 2 courses in the level = 50%
        await TakeQuizAsync(courseQuizId, studentId, pass: true);

        enrollment = await FindAsync<Enrollment>(enrollmentId);
        enrollment!.Status.ShouldBe(EnrollmentStatus.Completed);
        enrollment.LevelProgress.ShouldBe(50);

        // Level progress is shared by every enrollment of the student in that level
        var otherEnrollment = await FindAsync<Enrollment>(otherEnrollmentId);
        otherEnrollment!.Status.ShouldBe(EnrollmentStatus.Active);
        otherEnrollment.LevelProgress.ShouldBe(50);
    }
}
