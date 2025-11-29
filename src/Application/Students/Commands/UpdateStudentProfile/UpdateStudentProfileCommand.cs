using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.Students.Commands.UpdateStudentProfile;

public record UpdateStudentProfileCommand : IRequest<Guid>
{
    public string? Bio { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Address { get; init; }
    public string? Country { get; set; }
    public string? CountryCode { get; set; }
    public DateOnly? DateOfBirth { get; init; }
    public DateOnly? DateOfIslamConversion { get; set; }
}

public class UpdateStudentProfileCommandHandler : IRequestHandler<UpdateStudentProfileCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public UpdateStudentProfileCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(UpdateStudentProfileCommand request, CancellationToken cancellationToken)
    {
        var userIdString = _currentUser.Id ?? throw new UnauthorizedAccessException("User not authenticated");

        if (!Guid.TryParse(userIdString, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID format");
        }

        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(Student), userId.ToString());

        student.Bio = request.Bio;
        student.PhoneNumber = request.PhoneNumber;
        student.Address = request.Address;
        student.DateOfBirth = request.DateOfBirth;
        student.Country = request.Country;
        student.CountryCode = request.CountryCode;
        student.DateOfIslamConversion = request.DateOfIslamConversion;

        await _context.SaveChangesAsync(cancellationToken);

        return student.Id;
    }
}
