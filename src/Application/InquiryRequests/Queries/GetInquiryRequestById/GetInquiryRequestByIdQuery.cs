using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.InquiryRequests.Queries.GetInquiryRequestById;

public record GetInquiryRequestByIdQuery(Guid Id) : IRequest<Result<InquiryRequestDto?>>;

public class GetInquiryRequestByIdQueryHandler : IRequestHandler<GetInquiryRequestByIdQuery, Result<InquiryRequestDto?>>
{
    private readonly IApplicationDbContext _context;

    public GetInquiryRequestByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<InquiryRequestDto?>> Handle(GetInquiryRequestByIdQuery request, CancellationToken cancellationToken)
    {
        var data = await _context.InquiryRequests
            .Include(x => x.Requester)
            .Include(x => x.Moderator)
            .Where(i => i.Id == request.Id)
            .Select(s => new InquiryRequestDto {
            Id = s.Id,
            RequesterId = s.RequesterId,
            RequesterName = s.Requester != null ? s.Requester.FirstName + " " + s.Requester.LastName : string.Empty,
            ModeratorName = s.Moderator != null ? s.Moderator.FirstName + " " + s.Moderator.LastName : null,
            ModeratorId = s.ModeratorId,
            Topic = s.Topic,
            Message = s.Message,
            Languages = s.Languages,
            Status = s.Status,
            Response = s.Response,
            Created = s.Created
    })
            .FirstOrDefaultAsync(cancellationToken);
        return Result<InquiryRequestDto?>.Success(data);
    }
}
