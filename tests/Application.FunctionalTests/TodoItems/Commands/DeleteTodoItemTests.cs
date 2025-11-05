using AskAMuslimBackend.Application.TodoItems.Commands.CreateTodoItem;
using AskAMuslimBackend.Application.TodoItems.Commands.DeleteTodoItem;
using AskAMuslimBackend.Application.TodoLists.Commands.CreateTodoList;
using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.FunctionalTests.TodoItems.Commands;

using static Testing;

public class DeleteTodoItemTests : BaseTestFixture
{
    [Test]
    public async Task ShouldRequireValidTodoItemId()
    {
        var command = new DeleteTodoItemCommand(Guid.Parse("333e4567-e89b-12d7-a456-426614174000"));

        await Should.ThrowAsync<NotFoundException>(() => SendAsync(command));
    }

    [Test]
    public async Task ShouldDeleteTodoItem()
    {
        var listId = await SendAsync(new CreateTodoListCommand
        {
            Title = "New List"
        });

        var itemId = await SendAsync(new CreateTodoItemCommand
        {
            ListId = listId,
            Title = "New Item"
        });

        await SendAsync(new DeleteTodoItemCommand(itemId));

        var item = await FindAsync<TodoItem>(itemId);

        item.ShouldBeNull();
    }
}
