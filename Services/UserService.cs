using System.Collections.Concurrent;
using UserManagementAPI.Models;

namespace UserManagementAPI.Services;

public class UserService : IUserService
{
    private readonly ConcurrentDictionary<int, User> _users;
    private int _nextId;

    public UserService()
    {
        var initialUsers = new[]
        {
            new User(1, "Alice Johnson", "alice@example.com"),
            new User(2, "Bob Smith", "bob@example.com")
        };

        _users = new ConcurrentDictionary<int, User>(initialUsers.ToDictionary(user => user.Id));
        _nextId = _users.Keys.DefaultIfEmpty(0).Max() + 1;
    }

    public IReadOnlyCollection<User> GetAll() => _users.Values.OrderBy(user => user.Id).ToList();

    public User? GetById(int id) => _users.TryGetValue(id, out var user) ? user : null;

    public User Create(CreateUserRequest request)
    {
        var userId = _nextId;
        Interlocked.Increment(ref _nextId);
        var user = new User(userId, request.Name.Trim(), request.Email.Trim().ToLowerInvariant());
        _users[userId] = user;
        return user;
    }

    public User? Update(int id, UpdateUserRequest request)
    {
        if (!_users.ContainsKey(id))
        {
            return null;
        }

        var updatedUser = new User(id, request.Name.Trim(), request.Email.Trim().ToLowerInvariant());
        _users[id] = updatedUser;
        return updatedUser;
    }

    public bool Delete(int id) => _users.TryRemove(id, out _);
}
