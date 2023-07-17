using System.Diagnostics;
using EasilyNET.PropertyInjection.Attributes;

namespace PropertyInjectionWeb.Example.Controllers;

public interface ITest
{
    void Show();
}

public class Test : ITest
{
    [Injection]
    private readonly ILogger<Test> _logger;
    [Injection]
    public  ITest1 Test1 { get; set; }
    public void Show()
    {
        _logger?.LogInformation("Test");
        Test1?.Show();
    }
}

public interface ITest1
{
    void Show();
}

public class Test1 : ITest1
{
    [Injection] private  ILogger<Test> Logger { get; set; }

    public void Show()
    {
        Logger?.LogInformation("Test1");
    }
}


public class User
{

    public int Id { get; set; }

    public string Name { get; set; }

}

public class UserService<T>:IUserService<T>,IDisposable
{
    private static readonly List<T> _list = new List<T>();



    public void Add(T t)
    {
        _list.Add(t);
    }

    public IList<T> Get()
    {
        return _list;
    }

    public void Dispose()
    {
      
       Console.WriteLine("Dispose");
    }
}

public interface IUserService<T>
{
    void Add(T t);
    IList<T> Get();
}