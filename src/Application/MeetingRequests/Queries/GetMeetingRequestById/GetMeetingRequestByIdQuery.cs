using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.MeetingRequests.Queries.GetMeetingRequestById;

public record GetMeetingRequestByIdQuery(Guid Id) : IRequest<Result<MeetingRequestDto?>>;

public class GetMeetingRequestByIdQueryHandler : IRequestHandler<GetMeetingRequestByIdQuery, Result<MeetingRequestDto?>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetMeetingRequestByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<MeetingRequestDto?>> Handle(GetMeetingRequestByIdQuery request, CancellationToken cancellationToken)
    {
        var data = await _context.MeetingRequests
            .Include(x => x.Requester)
            .Include(x => x.Moderator)
            .Where(m => m.Id == request.Id)
            .ProjectTo<MeetingRequestDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
        return Result<MeetingRequestDto?>.Success(data);
    }
}
