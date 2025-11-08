using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<TodoList> TodoLists { get; }
    DbSet<TodoItem> TodoItems { get; }
    
    // User Management
    DbSet<Student> Students { get; }
    DbSet<Instructor> Instructors { get; }
    DbSet<Admin> Admins { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
