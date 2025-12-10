using AskAMuslimBackend.Application.TodoLists.Commands.CreateTodoList;
using AskAMuslimBackend.Application.TodoLists.Commands.DeleteTodoList;
using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.FunctionalTests.TodoLists.Commands;

using static Testing;

public class DeleteTodoListTests : BaseTestFixture
{
    [Test]
    public async Task ShouldRequireValidTodoListId()
    {
        var command = new DeleteTodoListCommand(Guid.Parse("333e4567-e89b-12d7-a456-426614174000"));
        await Should.ThrowAsync<NotFoundException>(() => SendAsync(command));
    }

    [Test]
    public async Task ShouldDeleteTodoList()
    {
        var listId = (await SendAsync(new CreateTodoListCommand
        {
            Title = "New List"
        })).Data;

        await SendAsync(new DeleteTodoListCommand(listId));

        var list = await FindAsync<TodoList>(listId);

        list.ShouldBeNull();
    }
}
