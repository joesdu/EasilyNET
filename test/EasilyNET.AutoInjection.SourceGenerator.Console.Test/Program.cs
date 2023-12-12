// See https://aka.ms/new-console-template for more information

using EasilyNET.AutoDependencyInjection.Core.Abstractions;
using EasilyNET.AutoDependencyInjection.Core.Attributes;
using Microsoft.Extensions.DependencyInjection;

internal class Program
{
    private static void Main(string[] args)
    {
        //IServiceCollection serviceCollection = new ServiceCollection();
        //var service = serviceCollection.AddAutodd();
        //var provider = service.BuildServiceProvider();
        //var test1 = provider.GetService<ITest<User>>();
        //Console.WriteLine(test1!.GetTest());
        //var test2 = provider.GetService<ITest1<User, User>>();
        //Console.WriteLine(test2!.GetTest());
        //Console.ReadKey();
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

[DependencyInjection(ServiceLifetime.Scoped)]
public class Test3 { }

public interface ITestTransient : IAA1p, ITransientDependency;

public interface ITestScoped : IScopedDependency;

public interface ITestSingleton : ISingletonDependency;

public class TestTransient : ITestTransient { }

public class TestTransient1 : ITestTransient { }

public class TestScoped : ITestScoped { }

public class TestScoped1 : ITestScoped { }

public class TestSingleton : ITestSingleton { }

public interface IAA;

public class AA : IAA { }

public interface IAA1p { }

public class User { }

public interface ITest<T> : ITransientDependency
{
    string GetTest();
}

public class Test<User> : ITest<User>
{
    public string GetTest() => "Test";
}

public interface ITest1<T, T1> : ITransientDependency
{
    string GetTest();
}

public class Test1<T, T1> : ITest1<T, T1>
{
    public string GetTest() => "Test1";
}

[DependencyInjection(ServiceLifetime.Scoped, AddSelf = true)]
public class Test4 { }

[DependencyInjection(ServiceLifetime.Scoped)]
public class Test5 : ITest5 { }

public interface ITest5 { }

[DependencyInjection(ServiceLifetime.Scoped)]
public abstract class Test6 { }

[DependencyInjection(ServiceLifetime.Scoped)]
public class Test7 : Test8, ITset7 { }

public interface ITset7 { }

public abstract class Test8 { }

[DependencyInjection(ServiceLifetime.Scoped)]
public class Test9 : Test10 { }

public class Test10 { }