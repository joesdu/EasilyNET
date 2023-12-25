// See https://aka.ms/new-console-template for more information

using EasilyNET.AutoDependencyInjection;
using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Core.Abstractions;
using EasilyNET.AutoDependencyInjection.Core.Attributes;
using EasilyNET.AutoDependencyInjection.Modules;
using EasilyNET.AutoInjection.SourceGenerator.Console.Test;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable CA1050

// ReSharper disable ClassNeverInstantiated.Global

internal class Program
{
    private static void Main()
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
            serviceProvider.GetService<ITest7>()?.GetTest();
            serviceProvider.GetService<Test9>()?.GetTest();
            serviceProvider.GetService<Test11>()?.GetTest();
            serviceProvider.GetService<Test13>()?.GetTest();
            serviceProvider.GetService<ITest14>()?.GetTest();
        }
        Console.ReadKey();
    }
}

public sealed class AppConsoleModule : AppModule
{
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        // TODO: @瓜哥,解决报错问题
        //1.重新编译一下
        //2.重启VS
        //3.添加EasilyNET.AutoInjection.SourceGenerator.Console.Test 引用 
        context.Services.AddAutoConsoleInjection();
        base.ConfigureServices(context);
    }
}

public interface ITestTransient : IAA1p, IAA, ITransientDependency
{
    new void GetTest(string name);
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
    public void GetTest(string name) => Console.WriteLine($"{nameof(TestTransient)}_{name}");
}

public class TestScoped : ITestScoped
{
    public void GetTest(string name) => Console.WriteLine($"{nameof(TestScoped)}_{name}");
}

public class TestScoped1 : ITestScoped
{
    public void GetTest(string name) => Console.WriteLine($"{nameof(TestScoped1)}_{name}");
}

public class TestSingleton : ITestSingleton
{
    public void GetTest(string name) => Console.WriteLine($"{nameof(TestSingleton)}_{name}");
}

public interface IAA
{
    void GetTest(string name);
}

public class AA : IAA
{
    public void GetTest(string name) => Console.WriteLine($"{nameof(AA)}_{name}");
}

public interface IAA1p
{
    void GetTest(string name);
}

public class User;

public interface ITest<in T> : ITransientDependency
{
    void GetTest(T name);
}

public class Test<T> : ITest<User>
{
    public void GetTest(User name) => Console.WriteLine($"{typeof(Test<User>).Name}_{name}");
}

public interface ITest1<in T, in T1> : ITransientDependency
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
    public void GetTest() => Console.WriteLine($"{nameof(Test3)}");
}

[DependencyInjection(ServiceLifetime.Scoped, AddSelf = true)]
public class Test4
{
    public void GetTest() => Console.WriteLine($"{nameof(Test4)}");
}

[DependencyInjection(ServiceLifetime.Scoped)]
public class Test5 : ITest5
{
    public void GetTest() => Console.WriteLine($"{nameof(Test5)}");
}

public interface ITest5
{
    void GetTest();
}

[DependencyInjection(ServiceLifetime.Scoped)]
public class Test6;

[DependencyInjection(ServiceLifetime.Scoped)]
public class Test7 : Test8, ITest7
{
    public void GetTest()
    {
        Console.WriteLine($"{nameof(Test7)}");
    }
}

public interface ITest7
{
    public void GetTest();
}

public class Test8;

[DependencyInjection(ServiceLifetime.Scoped)]
public class Test9 : Test10
{
    public void GetTest()
    {
        Console.WriteLine($"{nameof(Test9)}");
    }
}

public class Test10;

[DependencyInjection(ServiceLifetime.Scoped)]
public class Test11 : IScopedDependency
{
    public void GetTest()
    {
        Console.WriteLine($"{nameof(Test11)}");
    }
}

[IgnoreDependency, DependencyInjection(ServiceLifetime.Scoped)]
public class Test12 : IScopedDependency
{
    public void GetTest()
    {
        Console.WriteLine($"{nameof(Test12)}");
    }
}

[MyAttribute, DependencyInjection(ServiceLifetime.Scoped)]
public class Test13
{
    public void GetTest()
    {
        Console.WriteLine($"{nameof(Test13)}");
    }
}

public class Test14 : ITest14
{
    public void GetTest()
    {
        Console.WriteLine($"{nameof(Test14)}");
    }
}

public interface ITest14 : IScopedDependency
{
    void GetTest();
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class MyAttribute : Attribute;