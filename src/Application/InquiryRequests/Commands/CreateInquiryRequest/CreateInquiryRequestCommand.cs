using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;

namespace AskAMuslimBackend.Application.InquiryRequests.Commands.CreateInquiryRequest;

public record CreateInquiryRequestCommand : IRequest<Result<Guid>>
{
    public MeetingInquiryTopic Topic { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<Language> Languages { get; init; } = new();
}

public class CreateInquiryRequestCommandHandler : IRequestHandler<CreateInquiryRequestCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public CreateInquiryRequestCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Result<Guid>> Handle(CreateInquiryRequestCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_user.Id))
        {
            return Result<Guid>.Failure(new[] { "Unauthorized" });
        }

        var requesterGuid = Guid.Parse(_user.Id);

        var inquiry = new InquiryRequest
        {
            RequesterId = requesterGuid,
            Topic = request.Topic,
            Message = request.Message,
            Languages = request.Languages,
            Status = MeetingInquiryRequestStatus.Pending,
        };

        _context.InquiryRequests.Add(inquiry);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(inquiry.Id);
    }
}

public class CreateInquiryRequestCommandValidator : AbstractValidator<CreateInquiryRequestCommand>
{
    public CreateInquiryRequestCommandValidator()
    {
        RuleFor(x => x.Topic).IsInEnum();
        RuleFor(x => x.Message).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Languages).NotEmpty().WithMessage("At least one language must be specified.");
        RuleForEach(x => x.Languages).IsInEnum();
    }
}
