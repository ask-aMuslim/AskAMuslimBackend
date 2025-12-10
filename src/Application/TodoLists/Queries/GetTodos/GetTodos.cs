using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Security;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.TodoLists.Queries.GetTodos;

[Authorize]
public record GetTodosQuery : IRequest<Result<TodosVm>>;

public class GetTodosQueryHandler : IRequestHandler<GetTodosQuery, Result<TodosVm>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetTodosQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<TodosVm>> Handle(GetTodosQuery request, CancellationToken cancellationToken)
    {
        var data = new TodosVm
        {
            PriorityLevels = Enum.GetValues(typeof(PriorityLevel))
                .Cast<PriorityLevel>()
                .Select(p => new LookupDto { Id = new Guid(BitConverter.GetBytes((int)p).Concat(new byte[12]).ToArray()), Title = p.ToString() })
                .ToList(),

            Lists = await _context.TodoLists
                .AsNoTracking()
                .ProjectTo<TodoListDto>(_mapper.ConfigurationProvider)
                .OrderBy(t => t.Title)
                .ToListAsync(cancellationToken)
        };
        return Result<TodosVm>.Success(data);
    }
}
