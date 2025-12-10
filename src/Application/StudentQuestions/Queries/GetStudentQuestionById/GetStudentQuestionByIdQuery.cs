using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.StudentQuestions.Queries.GetStudentQuestionById;

public record GetStudentQuestionByIdQuery(Guid Id) : IRequest<Result<StudentQuestionDto?>>;

public class GetStudentQuestionByIdQueryHandler : IRequestHandler<GetStudentQuestionByIdQuery, Result<StudentQuestionDto?>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetStudentQuestionByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<StudentQuestionDto?>> Handle(GetStudentQuestionByIdQuery request, CancellationToken cancellationToken)
    {
        var question = await _context.StudentQuestions
            .Where(q => q.Id == request.Id)
            .ProjectTo<StudentQuestionDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
        
        return Result<StudentQuestionDto?>.Success(question);
    }
}
