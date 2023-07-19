using EasilyNET.AutoDependencyInjection.Core.Attributes;

// ReSharper disable ClassNeverInstantiated.Global

namespace WebApi.Test.Unit;

/// <summary>
/// ITest
/// </summary>
public interface ITest
{
    /// <summary>
    /// Show
    /// </summary>
    void Show();
}

/// <inheritdoc />
[DependencyInjection(ServiceLifetime.Scoped, AddSelf = true)]
public class Test : ITest
{
    [Injection]
    private readonly ILogger<Test>? _logger = null;

    [Injection]
    private ITest1? Test1 { get; } = null;

    /// <inheritdoc />
    public void Show()
    {
        _logger?.LogInformation("从Test输出日志");
        Test1?.Show();
    }
}

/// <summary>
/// ITest1
/// </summary>
public interface ITest1
{
    /// <summary>
    /// Show
    /// </summary>
    void Show();
}

/// <inheritdoc />
[DependencyInjection(ServiceLifetime.Scoped, AddSelf = true)]
public class Test1 : ITest1
{
    [Injection]
    private readonly ILogger<Test>? _logger = null;

    /// <inheritdoc />
    public void Show()
    {
        _logger?.LogInformation("从Test1输出日志");
    }
}

/// <summary>
/// User
/// </summary>
public class User
{
    /// <summary>
    /// ID
    /// </summary>
    public int Id { get; set; }
}

/// <inheritdoc cref="User" />
[DependencyInjection(ServiceLifetime.Scoped, AddSelf = true)]
public class UserService<T> : IUserService<T>, IDisposable
{
    private static readonly List<T> _list = new();

    /// <inheritdoc />
    public void Dispose() => Console.WriteLine("Dispose");

    /// <inheritdoc />
    public void Add(T t)
    {
        _list.Add(t);
    }

    /// <inheritdoc />
    public IList<T> Get() => _list;
}

/// <summary>
/// IUserService
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IUserService<T>
{
    /// <summary>
    /// Add
    /// </summary>
    /// <param name="t"></param>
    void Add(T t);

    /// <summary>
    /// GET
    /// </summary>
    /// <returns></returns>
    IList<T> Get();
}