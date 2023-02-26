using EasilyNET.AutoDependencyInjection.Abstractions;

namespace Test.ServiceModules;

/// <summary>
/// 测试模块
/// </summary>
public class Test : ITest, IScopedDependency
{
    /// <summary>
    /// Show
    /// </summary>
    public void Show()
    {
        Console.WriteLine("Test");
    }
}

/// <summary>
/// 测试
/// </summary>
public interface ITest
{
    /// <summary>
    /// Show
    /// </summary>
    void Show();
}
///// <summary>
///// 测试
///// </summary>
//[DependencyInjection(ServiceLifetime.Singleton)]
//public class Test2
//{
//    /// <summary>
//    /// Show
//    /// </summary>
//    public void Show()
//    {
//        Console.WriteLine("测试自动注入Test2");
//    }
//}