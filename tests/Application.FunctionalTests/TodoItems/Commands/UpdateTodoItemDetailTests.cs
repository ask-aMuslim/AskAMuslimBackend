using AskAMuslimBackend.Application.TodoItems.Commands.CreateTodoItem;
using AskAMuslimBackend.Application.TodoItems.Commands.UpdateTodoItem;
using AskAMuslimBackend.Application.TodoItems.Commands.UpdateTodoItemDetail;
using AskAMuslimBackend.Application.TodoLists.Commands.CreateTodoList;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.FunctionalTests.TodoItems.Commands;

using static Testing;

public class UpdateTodoItemDetailTests : BaseTestFixture
{
    [Test]
    public async Task ShouldRequireValidTodoItemId()
    {
        var command = new UpdateTodoItemCommand { Id = Guid.Parse("333e4567-e89b-12d7-a456-426614174000"), Title = "New Title" };

        await Should.ThrowAsync<NotFoundException>(() => SendAsync(command));
    }

    [Test]
    public async Task ShouldUpdateTodoItem()
    {
        var userId = await RunAsDefaultUserAsync();

        var listId = (await SendAsync(new CreateTodoListCommand
        {
            Title = "New List"
        })).Data;

        var itemId = (await SendAsync(new CreateTodoItemCommand
        {
            ListId = listId,
            Title = "New Item"
        })).Data;

        var listId2 = (await SendAsync(new CreateTodoListCommand
        {
            Title = "New List 2"
        })).Data;

        var command = new UpdateTodoItemDetailCommand
        {
            Id = itemId,
            ListId = listId,
            Note = "This is the note.",
            Priority = PriorityLevel.High
        };

        await SendAsync(command);

        var item = await FindAsync<TodoItem>(itemId);

        item.ShouldNotBeNull();
        item!.ListId.ShouldBe(command.ListId);
        item.Note.ShouldBe(command.Note);
        item.Priority.ShouldBe(command.Priority);
        item.LastModifiedBy.ShouldNotBeNull();
        item.LastModifiedBy.ShouldBe(userId);
        item.LastModified.ShouldBe(DateTime.Now, TimeSpan.FromMilliseconds(10000));
    }
}
