using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Lessons.Queries.GetLessonById;

public record GetLessonByIdQuery(Guid Id) : IRequest<Result<LessonBriefDto?>>;

public class GetLessonByIdQueryHandler : IRequestHandler<GetLessonByIdQuery, Result<LessonBriefDto?>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetLessonByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<LessonBriefDto?>> Handle(GetLessonByIdQuery request, CancellationToken cancellationToken)
    {
        var lesson = await _context.Lessons
            .Where(l => l.Id == request.Id)
            .ProjectTo<LessonBriefDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
        
        return Result<LessonBriefDto?>.Success(lesson);
    }
}
