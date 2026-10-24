using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;
using FluentValidation;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.StudentQuestions.Commands.CreateStudentQuestion;

public record CreateStudentQuestionCommand : IRequest<Result<Guid>>
{
    public Guid StudentId { get; init; }
    public Guid LessonId { get; init; }
    public string QuestionText { get; init; } = string.Empty;
    public double Timestamp { get; init; }
}

public class CreateStudentQuestionCommandHandler : IRequestHandler<CreateStudentQuestionCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private const string CONTACT_EMAIL = "info@ask-a-muslim.com";

    public CreateStudentQuestionCommandHandler(
        IApplicationDbContext context,
        IEmailService emailService,
        IBackgroundJobClient backgroundJobClient)
    {
        _context = context;
        _emailService = emailService;
        _backgroundJobClient = backgroundJobClient;
    }

    public async Task<Result<Guid>> Handle(CreateStudentQuestionCommand request, CancellationToken cancellationToken)
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

        // Fetch student and lesson details for the notification email
        var studentUserId = await _context.Students
            .AsNoTracking()
            .Where(s => s.Id == request.StudentId)
            .Select(s => s.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        var studentUser = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == studentUserId)
            .Select(u => new { u.FirstName, u.LastName, u.Email })
            .FirstOrDefaultAsync(cancellationToken);

        var lessonTitle = await _context.Lessons
            .AsNoTracking()
            .Where(l => l.Id == request.LessonId)
            .Select(l => l.Title)
            .FirstOrDefaultAsync(cancellationToken) ?? "Unknown Lesson";

        if (studentUser != null && !string.IsNullOrEmpty(studentUser.Email))
        {
            var subject = $"New Student Question - {lessonTitle}";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 5px;'>
                        <h2 style='color: #2c3e50; border-bottom: 2px solid #3498db; padding-bottom: 10px;'>New Student Question</h2>
                        
                        <div style='margin: 20px 0;'>
                            <p style='margin: 10px 0;'><strong>Student:</strong> {studentUser.FirstName} {studentUser.LastName}</p>
                            <p style='margin: 10px 0;'><strong>Email:</strong> {studentUser.Email}</p>
                            <p style='margin: 10px 0;'><strong>Lesson:</strong> {lessonTitle}</p>
                            <p style='margin: 10px 0;'><strong>Timestamp:</strong> {request.Timestamp} seconds</p>
                        </div>
                        
                        <div style='margin: 20px 0;'>
                            <h3 style='color: #2c3e50; font-size: 16px;'>Question:</h3>
                            <div style='background-color: #f8f9fa; padding: 15px; border-left: 4px solid #3498db; border-radius: 3px;'>
                                {request.QuestionText.Replace("\n", "<br/>")}
                            </div>
                        </div>
                        
                        <div style='margin-top: 20px; padding-top: 20px; border-top: 1px solid #ddd; color: #7f8c8d; font-size: 12px;'>
                            <p>This question was submitted on {DateTime.UtcNow:f} UTC</p>
                        </div>
                    </div>
                </body>
                </html>";

            _backgroundJobClient.Enqueue(() =>
                _emailService.SendEmailAsync(CONTACT_EMAIL, subject, body, true));
        }

        return Result<Guid>.Success(entity.Id);
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
