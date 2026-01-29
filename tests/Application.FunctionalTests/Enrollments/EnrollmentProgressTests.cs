using AskAMuslimBackend.Application.Enrollments.Commands.CreateEnrollment;
using AskAMuslimBackend.Application.Levels.Commands.CreateLevel;
using AskAMuslimBackend.Application.Courses.Commands.CreateCourse;
using AskAMuslimBackend.Application.Lessons.Commands.CreateLesson;
using AskAMuslimBackend.Application.Quizzes.Commands.CreateQuiz;
using AskAMuslimBackend.Application.QuizAttempts.Commands.CreateQuizAttempt;
using AskAMuslimBackend.Application.QuizAttempts.Commands.CompleteQuizAttempt;
using AskAMuslimBackend.Application.Questions.Commands.CreateQuestion;
using AskAMuslimBackend.Application.Options.Commands.CreateOption;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.FunctionalTests.Enrollments;

using static Testing;

public class EnrollmentProgressTests : BaseTestFixture
{
    private readonly Dictionary<Guid, (Guid questionId, Guid correctOptionId, Guid incorrectOptionId)> _quizQuestions = new();

    private async Task<Guid> CreateStudentAsync()
    {
        var student = new Student { UserId = Guid.NewGuid() };
        await AddAsync(student);
        return student.Id;
    }

    private async Task<(Guid levelId, Guid courseId, Guid lesson1Id, Guid lesson2Id)> CreateContentGraphAsync()
    {
        var levelResult = await SendAsync(new CreateLevelCommand
        {
            Title = "Level 1",
            Description = "Level description",
            Difficulty = LevelDifficulty.Beginner,
            Order = 1
        });
        var levelId = levelResult.Data;

        var courseResult = await SendAsync(new CreateCourseCommand
        {
            LevelId = levelId,
            Title = "Course 1",
            Description = "Course description",
            IsPublished = true,
            Order = 1,
            TagIds = new()
        });
        var courseId = courseResult.Data;

        var lesson1Result = await SendAsync(new CreateLessonCommand
        {
            CourseId = courseId,
            Title = "Lesson 1",
            Order = 1
        });
        var lesson1Id = lesson1Result.Data;

        var lesson2Result = await SendAsync(new CreateLessonCommand
        {
            CourseId = courseId,
            Title = "Lesson 2",
            Order = 2
        });
        var lesson2Id = lesson2Result.Data;

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
        var correctOptionId = correctOptionResult.Data;

        var incorrectOptionResult = await SendAsync(new CreateOptionCommand
        {
            QuestionId = questionId,
            Text = "Incorrect Answer",
            IsCorrect = false
        });
        var incorrectOptionId = incorrectOptionResult.Data;

        _quizQuestions[quizId] = (questionId, correctOptionId, incorrectOptionId);

        return quizId;
    }

    private async Task<Guid> EnrollAsync(Guid studentId, Guid courseId)
    {
        var result = await SendAsync(new CreateEnrollmentCommand
        {
            StudentId = studentId,
            CourseId = courseId
        });
        return result.Data;
    }

    private async Task<Guid> StartAttemptAsync(Guid quizId, Guid studentId)
    {
        var result = await SendAsync(new CreateQuizAttemptCommand
        {
            QuizId = quizId,
            StudentId = studentId
        });
        return result.Data;
    }

    private async Task<(Guid correctOptionId, Guid incorrectOptionId)> AddQuestionToQuizAsync(Guid quizId)
    {
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
        var correctOptionId = correctOptionResult.Data;

        var incorrectOptionResult = await SendAsync(new CreateOptionCommand
        {
            QuestionId = questionId,
            Text = "Incorrect Answer",
            IsCorrect = false
        });
        var incorrectOptionId = incorrectOptionResult.Data;

        return (correctOptionId, incorrectOptionId);
    }

    private async Task CompleteAttemptAsync(Guid attemptId, Guid quizId, bool pass)
    {
        var (questionId, correctOptionId, incorrectOptionId) = _quizQuestions[quizId];
        
        await SendAsync(new CompleteQuizAttemptCommand
        {
            AttemptId = attemptId,
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

    [Test]
    public async Task Should_Update_CourseProgress_From_Course_And_Lesson_Quizzes()
    {
        var studentId = await CreateStudentAsync();
        var (levelId, courseId, lesson1Id, lesson2Id) = await CreateContentGraphAsync();

        // Create 1 course quiz + 2 lesson quizzes
        var courseQuizId = await CreateQuizAsync(QuizTargetType.Course, courseId: courseId);
        var lessonQuiz1Id = await CreateQuizAsync(QuizTargetType.Lesson, lessonId: lesson1Id);
        var lessonQuiz2Id = await CreateQuizAsync(QuizTargetType.Lesson, lessonId: lesson2Id);

        var enrollmentId = await EnrollAsync(studentId, courseId);

        var enrollment = await FindAsync<Enrollment>(enrollmentId);
        enrollment!.CourseProgress.ShouldBe(0);
        enrollment.LevelProgress.ShouldBe(0);

        // Pass course quiz => 1/3 = 33.33%
        var attempt1 = await StartAttemptAsync(courseQuizId, studentId);
        await CompleteAttemptAsync(attempt1, courseQuizId, true);

        enrollment = await FindAsync<Enrollment>(enrollmentId);
        enrollment!.CourseProgress.ShouldBe(33.33);
        enrollment.Status.ShouldBe(EnrollmentStatus.Active);

        // Fail lesson 1 quiz => no change
        var attempt2 = await StartAttemptAsync(lessonQuiz1Id, studentId);
        await CompleteAttemptAsync(attempt2, lessonQuiz1Id, false);

        enrollment = await FindAsync<Enrollment>(enrollmentId);
        enrollment!.CourseProgress.ShouldBe(33.33);

        // Pass lesson 1 quiz => 2/3 = 66.67%
        var attempt3 = await StartAttemptAsync(lessonQuiz1Id, studentId);
        await CompleteAttemptAsync(attempt3, lessonQuiz1Id, true);

        enrollment = await FindAsync<Enrollment>(enrollmentId);
        enrollment!.CourseProgress.ShouldBe(66.67);
        enrollment.Status.ShouldBe(EnrollmentStatus.Active);

        // Duplicate pass on same quiz => progress unchanged
        var attempt4 = await StartAttemptAsync(lessonQuiz1Id, studentId);
        await CompleteAttemptAsync(attempt4, lessonQuiz1Id, true);

        enrollment = await FindAsync<Enrollment>(enrollmentId);
        enrollment!.CourseProgress.ShouldBe(66.67);
    }

    [Test]
    public async Task Should_Complete_Course_At_100_And_Update_LevelProgress()
    {
        var studentId = await CreateStudentAsync();
        var (levelId, courseId, lesson1Id, lesson2Id) = await CreateContentGraphAsync();

        // Add another course in same level (not completed) to observe level progress
        var otherCourseResult = await SendAsync(new CreateCourseCommand
        {
            LevelId = levelId,
            Title = "Course 2",
            IsPublished = true,
            Order = 2,
            TagIds = new()
        });
        var otherCourseId = otherCourseResult.Data;

        var otherEnrollmentId = await EnrollAsync(studentId, otherCourseId);

        // Create quizzes for first course
        var courseQuizId = await CreateQuizAsync(QuizTargetType.Course, courseId: courseId);
        var lessonQuiz1Id = await CreateQuizAsync(QuizTargetType.Lesson, lessonId: lesson1Id);
        var lessonQuiz2Id = await CreateQuizAsync(QuizTargetType.Lesson, lessonId: lesson2Id);

        var enrollmentId = await EnrollAsync(studentId, courseId);

        // Pass all quizzes to reach 100%
        var a1 = await StartAttemptAsync(courseQuizId, studentId);
        await CompleteAttemptAsync(a1, courseQuizId, true);
        var a2 = await StartAttemptAsync(lessonQuiz1Id, studentId);
        await CompleteAttemptAsync(a2, lessonQuiz1Id, true);
        var a3 = await StartAttemptAsync(lessonQuiz2Id, studentId);
        await CompleteAttemptAsync(a3, lessonQuiz2Id, true);

        var enrollment = await FindAsync<Enrollment>(enrollmentId);
        enrollment!.CourseProgress.ShouldBe(100);
        enrollment.Status.ShouldBe(EnrollmentStatus.Completed);

        // Level progress should be 50% because 1 of 2 courses in level is completed
        enrollment.LevelProgress.ShouldBe(50);

        // Create a level-only quiz and complete it; level progress should recompute (still 50%)
        var levelQuizId = await CreateQuizAsync(QuizTargetType.Level, levelId: levelId);
        var la = await StartAttemptAsync(levelQuizId, studentId);
        await CompleteAttemptAsync(la, levelQuizId, true);

        enrollment = await FindAsync<Enrollment>(enrollmentId);
        enrollment!.LevelProgress.ShouldBe(50);

        // Complete other course as well (with zero quizzes it stays 0, so add a quiz to other course)
        var otherLessonResult = await SendAsync(new CreateLessonCommand
        {
            CourseId = otherCourseId,
            Title = "Other Lesson",
            Order = 1
        });
        var otherLessonId = otherLessonResult.Data;
        var otherLessonQuizId = await CreateQuizAsync(QuizTargetType.Lesson, lessonId: otherLessonId);
        var otherCourseQuizAttempt = await StartAttemptAsync(otherLessonQuizId, studentId);
        await CompleteAttemptAsync(otherCourseQuizAttempt, otherLessonQuizId, true);

        // Progress for the other course now 100% and status completed, which should raise level progress to 100 for both enrollments in this level
        var otherEnrollment = await FindAsync<Enrollment>(otherEnrollmentId);
        otherEnrollment!.CourseProgress.ShouldBe(100);
        otherEnrollment.Status.ShouldBe(EnrollmentStatus.Completed);

        enrollment = await FindAsync<Enrollment>(enrollmentId);
        enrollment!.LevelProgress.ShouldBe(100);
    }
}
