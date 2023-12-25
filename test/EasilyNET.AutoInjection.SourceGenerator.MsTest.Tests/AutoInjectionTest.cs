using EasilyNET.AutoDependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace EasilyNET.AutoInjection.SourceGenerator.MsTest.Tests;

/// <summary>
/// 单元测试
/// </summary>
[TestClass]
public class AutoInjectionTest
{
    /// <summary>
    /// </summary>
    [TestMethod]
    public void TestTransient()
    {
        using var application = ApplicationFactory.Create<TestAppModule>();
        var test = application.ServiceProvider!.GetService<ITestTransient>();
        var re = test?.GetTest("大黄瓜");
        // Assert
        Assert.IsTrue(re == $"{nameof(ITestTransient)}_大黄瓜");
    }

    /// <summary>
    /// </summary>
    [TestMethod]
    public void TestScoped()
    {
        using var application = ApplicationFactory.Create<TestAppModule>();
        var test = application.ServiceProvider!.GetService<ITestScoped>();
        var re = test?.GetTest("大黄瓜");
        // Assert
        Assert.IsTrue(re == $"{nameof(ITestScoped)}_大黄瓜");
    }

    /// <summary>
    /// </summary>
    [TestMethod]
    public void TestSingleton()
    {
        using var application = ApplicationFactory.Create<TestAppModule>();
        var test = application.ServiceProvider!.GetService<ITestSingleton>();
        var re = test?.GetTest("大黄瓜");
        // Assert
        Assert.IsTrue(re == $"{nameof(ITestSingleton)}_大黄瓜");
    }
}

/// <summary>
/// </summary>
public interface ITestTransient : ITransientDependency
{
    /// <summary>
    /// </summary>
    /// <param name="name"></param>
    string GetTest(string name);
}

/// <summary>
/// </summary>
public class TestTransient : ITestTransient
{
    /// <summary>
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public string GetTest(string name) => $"{nameof(ITestTransient)}_{name}";
}

/// <summary>
/// </summary>
public interface ITestScoped : IScopedDependency
{
    /// <summary>
    /// </summary>
    /// <param name="name"></param>
    string GetTest(string name);
}

/// <summary>
/// </summary>
public class TestScoped : ITestScoped
{
    /// <summary>
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public string GetTest(string name) => $"{nameof(ITestScoped)}_{name}";
}

/// <summary>
/// </summary>
public interface ITestSingleton : ISingletonDependency
{
    /// <summary>
    /// </summary>
    /// <param name="name"></param>
    string GetTest(string name);
}

/// <summary>
/// </summary>
public class TestSingleton : ITestSingleton
{
    /// <summary>
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public string GetTest(string name) => $"{nameof(ITestSingleton)}_{name}";
}