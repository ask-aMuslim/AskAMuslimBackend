using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.TodoLists.Commands.UpdateTodoList;

public record UpdateTodoListCommand : IRequest<Result>
{
    public Guid Id { get; init; }

    public string? Title { get; init; }
}

public class UpdateTodoListCommandHandler : IRequestHandler<UpdateTodoListCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateTodoListCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UpdateTodoListCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.TodoLists
            .FindAsync(new object[] { request.Id }, cancellationToken);

        if (entity == null) return Result.Failure(new[] { "TodoList not found" });

        entity.Title = request.Title;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
