using EasilyNET.Ipc.Server.Sample.Models;
using System.Collections.Concurrent;

namespace EasilyNET.Ipc.Server.Sample.Services;

/// <summary>
/// 用户存储服务
/// </summary>
public interface IUserStorage
{
    /// <summary>
    /// 获取所有用户
    /// </summary>
    /// <returns></returns>
    List<User> GetAllUsers();

    /// <summary>
    /// 根据ID获取用户
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    User? GetUser(int id);

    /// <summary>
    /// 添加用户
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    User AddUser(User user);

    /// <summary>
    /// 更新用户
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    User? UpdateUser(User user);

    /// <summary>
    /// 删除用户
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    bool DeleteUser(int id);

    /// <summary>
    /// 获取下一个ID
    /// </summary>
    /// <returns></returns>
    int GetNextId();
}

/// <summary>
/// 内存用户存储实现
/// </summary>
public class UserStorage : IUserStorage
{
    private readonly ConcurrentDictionary<int, User> _users = new();
    private int _nextId = 1;

    public UserStorage()
    {
        // 初始化一些测试数据
        AddUser(new User
        {
            Id = GetNextId(),
            Name = "测试用户1",
            Email = "test1@example.com",
            Phone = "13800138001",
            CreatedAt = DateTime.Now,
            IsActive = true
        });

        AddUser(new User
        {
            Id = GetNextId(),
            Name = "测试用户2",
            Email = "test2@example.com",
            Phone = "13800138002",
            CreatedAt = DateTime.Now,
            IsActive = true
        });
    }

    public List<User> GetAllUsers()
    {
        return _users.Values.Where(u => u.IsActive).ToList();
    }

    public User? GetUser(int id)
    {
        _users.TryGetValue(id, out var user);
        return user?.IsActive == true ? user : null;
    }

    public User AddUser(User user)
    {
        if (user.Id == 0)
        {
            user.Id = GetNextId();
        }
        user.CreatedAt = DateTime.Now;
        user.IsActive = true;

        _users.TryAdd(user.Id, user);
        return user;
    }

    public User? UpdateUser(User user)
    {
        if (_users.TryGetValue(user.Id, out var existingUser) && existingUser.IsActive)
        {
            user.CreatedAt = existingUser.CreatedAt;
            user.UpdatedAt = DateTime.Now;
            _users.TryUpdate(user.Id, user, existingUser);
            return user;
        }
        return null;
    }

    public bool DeleteUser(int id)
    {
        if (_users.TryGetValue(id, out var user) && user.IsActive)
        {
            user.IsActive = false;
            user.UpdatedAt = DateTime.Now;
            return true;
        }
        return false;
    }

    public int GetNextId()
    {
        return Interlocked.Increment(ref _nextId);
    }
}
