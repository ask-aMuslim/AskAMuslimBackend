using AskAMuslimBackend.Application.Common.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Options.Queries.GetOptionsByQuestion;

public record GetOptionsByQuestionQuery(Guid QuestionId) : IRequest<List<OptionBriefDto>>;

public class GetOptionsByQuestionQueryHandler : IRequestHandler<GetOptionsByQuestionQuery, List<OptionBriefDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetOptionsByQuestionQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<OptionBriefDto>> Handle(GetOptionsByQuestionQuery request, CancellationToken cancellationToken)
    {
        return await _context.Options
            .Where(o => o.QuestionId == request.QuestionId)
            .ProjectTo<OptionBriefDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
    }
}
