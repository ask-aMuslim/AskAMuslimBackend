using AskAMuslimBackend.Application.Common.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.StudentQuestions.Queries.GetStudentQuestionById;

public record GetStudentQuestionByIdQuery(Guid Id) : IRequest<StudentQuestionDto?>;

public class GetStudentQuestionByIdQueryHandler : IRequestHandler<GetStudentQuestionByIdQuery, StudentQuestionDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetStudentQuestionByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<StudentQuestionDto?> Handle(GetStudentQuestionByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.StudentQuestions
            .Where(q => q.Id == request.Id)
            .ProjectTo<StudentQuestionDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
