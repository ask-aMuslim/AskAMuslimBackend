using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Options.Queries.GetOptionsByQuestion;

public record GetOptionsByQuestionQuery(Guid QuestionId) : IRequest<Result<List<OptionBriefDto>>>;

public class GetOptionsByQuestionQueryHandler : IRequestHandler<GetOptionsByQuestionQuery, Result<List<OptionBriefDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetOptionsByQuestionQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<List<OptionBriefDto>>> Handle(GetOptionsByQuestionQuery request, CancellationToken cancellationToken)
    {
        var data = await _context.Options
            .Where(o => o.QuestionId == request.QuestionId)
            .ProjectTo<OptionBriefDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
        return Result<List<OptionBriefDto>>.Success(data);
    }
}
