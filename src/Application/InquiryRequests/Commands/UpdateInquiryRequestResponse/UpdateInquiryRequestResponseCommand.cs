using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Security;
using AskAMuslimBackend.Domain.Constants;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.InquiryRequests.Commands.UpdateInquiryRequestResponse;

[Authorize(Roles = Roles.Administrator + ",Admin")]
public record UpdateInquiryRequestResponseCommand : IRequest<Result>
{
    public Guid Id { get; init; }
    public string Response { get; init; } = string.Empty;
}

public class UpdateInquiryRequestResponseCommandHandler : IRequestHandler<UpdateInquiryRequestResponseCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateInquiryRequestResponseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UpdateInquiryRequestResponseCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.InquiryRequests.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (entity == null) return Result.Failure(new[] { "Inquiry request not found" });

        entity.Response = request.Response;
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public class UpdateInquiryRequestResponseCommandValidator : AbstractValidator<UpdateInquiryRequestResponseCommand>
{
    public UpdateInquiryRequestResponseCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Response).NotEmpty().MaximumLength(4000);
    }
}
