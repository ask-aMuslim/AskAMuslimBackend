using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.Students.Commands.UpdateStudentProfile;

public record UpdateStudentProfileCommand : IRequest<Result<Guid>>
{
    public string? Bio { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Address { get; init; }
    public string? Country { get; set; }
    public string? CountryCode { get; set; }
    public DateOnly? DateOfBirth { get; init; }
    public DateOnly? DateOfIslamConversion { get; set; }
}

public class UpdateStudentProfileCommandHandler : IRequestHandler<UpdateStudentProfileCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public UpdateStudentProfileCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(UpdateStudentProfileCommand request, CancellationToken cancellationToken)
    {
        var userIdString = _currentUser.Id;
        
        if (string.IsNullOrEmpty(userIdString))
        {
            return Result<Guid>.Failure(new[] { "User not authenticated" });
        }

        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result<Guid>.Failure(new[] { "Invalid user ID format" });
        }

        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
            
        if (student == null)
        {
            return Result<Guid>.Failure(new[] { $"Student with user ID {userId} not found" });
        }

        student.Bio = request.Bio;
        student.PhoneNumber = request.PhoneNumber;
        student.Address = request.Address;
        student.DateOfBirth = request.DateOfBirth;
        student.Country = request.Country;
        student.CountryCode = request.CountryCode;
        student.DateOfIslamConversion = request.DateOfIslamConversion;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(student.Id);
    }
}
