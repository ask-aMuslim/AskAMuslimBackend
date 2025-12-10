using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.TodoLists.Commands.DeleteTodoList;

public record DeleteTodoListCommand(Guid Id) : IRequest<Result>;

public class DeleteTodoListCommandHandler : IRequestHandler<DeleteTodoListCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteTodoListCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteTodoListCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.TodoLists
            .Where(l => l.Id == request.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (entity == null) return Result.Failure(new[] { "TodoList not found" });

        _context.TodoLists.Remove(entity);

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
