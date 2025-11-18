using AskAMuslimBackend.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.MeetingRequests.Queries.GetMeetingRequestById;

public record GetMeetingRequestByIdQuery(Guid Id) : IRequest<MeetingRequestDto?>;

public class GetMeetingRequestByIdQueryHandler : IRequestHandler<GetMeetingRequestByIdQuery, MeetingRequestDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetMeetingRequestByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<MeetingRequestDto?> Handle(GetMeetingRequestByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.MeetingRequests
            .Where(m => m.Id == request.Id)
            .ProjectTo<MeetingRequestDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
