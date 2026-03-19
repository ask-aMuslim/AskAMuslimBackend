using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.InquiryRequests.Queries.GetInquiryRequestsByRequester;

public record GetInquiryRequestsByRequesterQuery(Guid RequesterId, MeetingInquiryRequestStatus? Status = null) : IRequest<Result<List<InquiryRequestDto>>>;

public class GetInquiryRequestsByRequesterQueryHandler : IRequestHandler<GetInquiryRequestsByRequesterQuery, Result<List<InquiryRequestDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetInquiryRequestsByRequesterQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<InquiryRequestDto>>> Handle(GetInquiryRequestsByRequesterQuery request, CancellationToken cancellationToken)
    {
        var query = _context.InquiryRequests
            .Include(x => x.Requester)
            .AsQueryable()
            .Where(i => i.RequesterId == request.RequesterId);
        if (request.Status.HasValue)
        {
            query = query.Where(i => i.Status == request.Status.Value);
        }
        var data = await query.Select(s => new InquiryRequestDto {
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
    }).ToListAsync(cancellationToken);
        return Result<List<InquiryRequestDto>>.Success(data);
    }
}
