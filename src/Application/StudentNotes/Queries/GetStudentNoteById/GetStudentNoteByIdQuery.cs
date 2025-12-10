using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.StudentNotes.Queries.GetStudentNoteById;

public record GetStudentNoteByIdQuery(Guid Id) : IRequest<Result<StudentNoteDto?>>;

public class GetStudentNoteByIdQueryHandler : IRequestHandler<GetStudentNoteByIdQuery, Result<StudentNoteDto?>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetStudentNoteByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<StudentNoteDto?>> Handle(GetStudentNoteByIdQuery request, CancellationToken cancellationToken)
    {
        var note = await _context.StudentNotes
            .Where(n => n.Id == request.Id)
            .ProjectTo<StudentNoteDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
        
        return Result<StudentNoteDto?>.Success(note);
    }
}
