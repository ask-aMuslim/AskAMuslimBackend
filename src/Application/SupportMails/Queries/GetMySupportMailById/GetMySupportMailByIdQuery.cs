using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.SupportMails.Common;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.SupportMails.Queries.GetMySupportMailById;

public record GetMySupportMailByIdQuery(Guid Id) : IRequest<Result<MySupportMailDetailDto>>;

public class GetMySupportMailByIdQueryHandler : IRequestHandler<GetMySupportMailByIdQuery, Result<MySupportMailDetailDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetMySupportMailByIdQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Result<MySupportMailDetailDto>> Handle(GetMySupportMailByIdQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_user.Id))
        {
            return Result<MySupportMailDetailDto>.Failure(new[] { "Unauthorized" });
        }

        var requesterId = Guid.Parse(_user.Id);

        var mail = await _context.SupportMails
            .AsNoTracking()
            .Where(m => m.Id == request.Id && m.RequesterId == requesterId)
            .Select(m => new MySupportMailDetailDto
            {
                Id = m.Id,
                Number = m.Number,
                Topic = m.Topic,
                Subject = m.Subject,
                Status = m.Status,
                Created = m.Created,
                LastMessageAt = m.LastMessageAt,
                Messages = m.Messages
                    .OrderBy(msg => msg.Created)
                    .Select(msg => new SupportMailMessageDto
                    {
                        Id = msg.Id,
                        SenderType = msg.SenderType,
                        // Staff identity is not exposed to the requester; the app shows "Support Team".
                        SenderName = msg.SenderType == SupportMailSenderType.Requester && msg.Sender != null
                            ? msg.Sender.FirstName + " " + msg.Sender.LastName
                            : null,
                        SenderProfilePictureUrl = msg.SenderType == SupportMailSenderType.Requester && msg.Sender != null
                            ? msg.Sender.ImageUrl
                            : null,
                        Body = msg.Body,
                        Created = msg.Created
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        return mail == null
            ? Result<MySupportMailDetailDto>.Failure(new[] { "Mail not found" })
            : Result<MySupportMailDetailDto>.Success(mail);
    }
}
