using LoginService.Models;

namespace LoginService.Data;

public interface IUserStore
{
    UserAccount? FindByUserName(string userName);
    UserAccount Add(UserAccount user);
    IReadOnlyList<UserAccount> GetAll();
}

public sealed class InMemoryUserStore : IUserStore
{
    private readonly Dictionary<string, UserAccount> _users = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public UserAccount? FindByUserName(string userName)
    {
        lock (_lock)
        {
            return _users.TryGetValue(userName, out var user) ? user : null;
        }
    }

    public UserAccount Add(UserAccount user)
    {
        lock (_lock)
        {
            _users[user.UserName] = user;
            return user;
        }
    }

    public IReadOnlyList<UserAccount> GetAll()
    {
        lock (_lock)
        {
            return _users.Values.ToList();
        }
    }
}
