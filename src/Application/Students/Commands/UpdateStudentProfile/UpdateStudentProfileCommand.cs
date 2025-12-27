using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.Students.Commands.UpdateStudentProfile;

// This command is deprecated. Use StudentProfiles.Commands.UpdateStudentProfile instead.
// Kept for backward compatibility, redirects to StudentProfile functionality.
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
    private readonly IMediator _mediator;

    public UpdateStudentProfileCommandHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<Result<Guid>> Handle(UpdateStudentProfileCommand request, CancellationToken cancellationToken)
    {
        // Redirect to the new StudentProfile command
        var profileCommand = new UpdateStudentProfileCommand
        {
            Bio = request.Bio,
            PhoneNumber = request.PhoneNumber,
            Address = request.Address,
            Country = request.Country,
            CountryCode = request.CountryCode,
            DateOfBirth = request.DateOfBirth,
            DateOfIslamConversion = request.DateOfIslamConversion
        };

        return await _mediator.Send(profileCommand, cancellationToken);
    }
}
