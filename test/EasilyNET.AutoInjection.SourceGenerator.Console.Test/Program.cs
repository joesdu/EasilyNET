// See https://aka.ms/new-console-template for more information

using EasilyNET.AutoDependencyInjection;
using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Core.Abstractions;
using EasilyNET.AutoDependencyInjection.Core.Attributes;
using EasilyNET.AutoDependencyInjection.Modules;
using EasilyNET.AutoInjection.SourceGenerator.Console.Test;
using Microsoft.Extensions.DependencyInjection;

internal class Program
{
    private static void Main(string[] args)
    {
        using (var provider = ApplicationFactory.Create<AppConsoleModule>())
        {
            var serviceProvider = provider.ServiceProvider!;
            serviceProvider.GetService<ITestTransient>()?.GetTest("ITestTransient");
            serviceProvider.GetService<IAA1p>()?.GetTest("IAA1p");
            serviceProvider.GetService<IAA>()?.GetTest("IAA");
            foreach (var testScoped in serviceProvider.GetServices<ITestScoped>())
            {
                testScoped.GetTest("ITestScoped");
            }
            serviceProvider.GetService<ITestSingleton>()?.GetTest("TestSingleton");
            serviceProvider.GetService<ITest<int>>()?.GetTest(18);
            serviceProvider.GetService<ITest1<int, int>>()?.GetTest(18, 20);
            serviceProvider.GetService<Test3>()?.GetTest();
            serviceProvider.GetService<Test4>()?.GetTest();
            serviceProvider.GetService<ITest5>()?.GetTest();
            serviceProvider.GetService<ITset7>()?.GetTest();
            serviceProvider.GetService<Test9>()?.GetTest();
            serviceProvider.GetService<Test11>()?.GetTest();
            serviceProvider.GetService<Test13>()?.GetTest();
            serviceProvider.GetService<ITest14>()?.GetTest();
        }
        //IServiceCollection serviceCollection = new ServiceCollection();
        //var service = serviceCollection.AddAutoInjection();
        //var provider = service.BuildServiceProvider();
        Console.ReadKey();
    }
}

public sealed class AppConsoleModule : AppModule
{
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        context.Services.AddAutoConsoleInjection();
        base.ConfigureServices(context);
    }
}

//public static class _Dependency
//{
//    /// <summary>
//    /// 自动注入
//    /// </summary>
//    /// <param name="services"></param>
//    /// <returns></returns>
//    public static IServiceCollection AddAutoInjection1(this IServiceCollection services)
//    {
//        services.Add(new(typeof(ITestTransient), typeof(TestTransient), ServiceLifetime.Singleton));
//        return services;
//    }
//}
//

public interface ITestTransient : IAA1p, IAA, ITransientDependency
{
    void GetTest(string name);
}

public interface ITestScoped : IScopedDependency
{
    void GetTest(string name);
}

public interface ITestSingleton : ISingletonDependency
{
    void GetTest(string name);
}

public class TestTransient : ITestTransient
{
    public void GetTest(string name) => Console.WriteLine($"{typeof(TestTransient).Name}_{name}");
}

public class TestScoped : ITestScoped
{
    public void GetTest(string name) => Console.WriteLine($"{typeof(TestScoped).Name}_{name}");
}

public class TestScoped1 : ITestScoped
{
    public void GetTest(string name) => Console.WriteLine($"{typeof(TestScoped1).Name}_{name}");
}

public class TestSingleton : ITestSingleton
{
    public void GetTest(string name) => Console.WriteLine($"{typeof(TestSingleton).Name}_{name}");
}

public interface IAA
{
    void GetTest(string name);
}

public class AA : IAA
{
    public void GetTest(string name) => Console.WriteLine($"{typeof(AA).Name}_{name}");
}

public interface IAA1p
{
    void GetTest(string name);
}

public class User { }

public interface ITest<T> : ITransientDependency
{
    void GetTest(T name);
}

public class Test<User> : ITest<User>
{
    public void GetTest(User name) => Console.WriteLine($"{typeof(Test<User>).Name}_{name}");
}

public interface ITest1<T, T1> : ITransientDependency
{
    void GetTest(T t, T1 t1);
}

public class Test1<T, T1> : ITest1<T, T1>
{
    public void GetTest(T t, T1 t1) => Console.WriteLine($"{typeof(Test1<T, T1>).Name}_{t},{t1}");
}

[DependencyInjection(ServiceLifetime.Scoped)]
public class Test3
{
    public void GetTest() => Console.WriteLine($"{typeof(Test3).Name}");
}

[DependencyInjection(ServiceLifetime.Scoped, AddSelf = true)]
public class Test4
{
    public void GetTest() => Console.WriteLine($"{typeof(Test4).Name}");
}

[DependencyInjection(ServiceLifetime.Scoped)]
public class Test5 : ITest5
{
    public void GetTest() => Console.WriteLine($"{typeof(Test5).Name}");
}

public interface ITest5
{
    void GetTest();
}

[DependencyInjection(ServiceLifetime.Scoped)]
public abstract class Test6 { }

[DependencyInjection(ServiceLifetime.Scoped)]
public class Test7 : Test8, ITset7
{
    public void GetTest()
    {
        Console.WriteLine($"{typeof(Test7).Name}");
    }
}

public interface ITset7
{
    public void GetTest();
}

public abstract class Test8 { }

[DependencyInjection(ServiceLifetime.Scoped)]
public class Test9 : Test10
{
    public void GetTest()
    {
        Console.WriteLine($"{typeof(Test9).Name}");
    }
}

public class Test10 { }

[DependencyInjection(ServiceLifetime.Scoped)]
public class Test11 : IScopedDependency
{
    public void GetTest()
    {
        Console.WriteLine($"{typeof(Test11).Name}");
    }
}

[IgnoreDependency, DependencyInjection(ServiceLifetime.Scoped)]
public class Test12 : IScopedDependency
{
    public void GetTest()
    {
        Console.WriteLine($"{typeof(Test12).Name}");
    }
}

[MyAttribute, DependencyInjection(ServiceLifetime.Scoped)]
public class Test13
{
    public void GetTest()
    {
        Console.WriteLine($"{typeof(Test13).Name}");
    }
}

public class Test14 : ITest14
{
    public void GetTest()
    {
        Console.WriteLine($"{typeof(Test14).Name}");
    }
}

public interface ITest14 : IScopedDependency
{
    void GetTest();
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class MyAttribute : Attribute { }