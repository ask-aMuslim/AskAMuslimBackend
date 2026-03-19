using System.Linq.Expressions;
﻿using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.Common.Models;

public class LookupDto
{
    public Guid Id { get; init; }

    public string? Title { get; init; }
}
