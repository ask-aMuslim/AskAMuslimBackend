using System.Linq.Expressions;
﻿using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.TodoItems.Queries.GetTodoItemsWithPagination;

public class TodoItemBriefDto
{
    public Guid Id { get; init; }

    public Guid ListId { get; init; }

    public string? Title { get; init; }

    public bool Done { get; init; }
}
