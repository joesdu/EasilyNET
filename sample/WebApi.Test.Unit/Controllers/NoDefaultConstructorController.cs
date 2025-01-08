using EasilyNET.AutoDependencyInjection.Core.Attributes;
using EasilyNET.Core.Misc;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Test.Unit.Controllers;

/// <summary>
/// test controller without default constructor
/// </summary>
/// <param name="provider"></param>
[Route("api/[controller]")]
[ApiController]
public class NoDefaultConstructorController(IServiceProvider provider) : ControllerBase
{
    /// <summary>
    /// Get
    /// </summary>
    /// <returns></returns>
    [HttpGet("TestNoDefaultCtor")]
    public string Get()
    {
        var test = provider.ResolveNamed<ITestNoDefaultConstructor>("TestNoDefaultConstructor", new()
        {
            { "str", "Jack" },
            { "str2", "Rose" }
        });
        return test.SayHello();
    }
}

/// <summary>
/// Test class without default constructor
/// </summary>
[DependencyInjection(ServiceLifetime.Singleton)]
// ReSharper disable once ClassNeverInstantiated.Global
public sealed class InjectClass
{
    /// <summary>
    /// get hello
    /// </summary>
    /// <returns></returns>
    // ReSharper disable once MemberCanBeMadeStatic.Global
    public string GetHello(params IEnumerable<string> str) => $"Hello, {str.Join()}";
}

/// <summary>
/// Test class without default constructor
/// </summary>
[DependencyInjection(ServiceLifetime.Transient, ServiceKey = "TestNoDefaultConstructor")]
// ReSharper disable once UnusedType.Global
public sealed class TestNoDefaultConstructor(InjectClass jc, string str, string str2) : ITestNoDefaultConstructor
{
    /// <summary>
    /// Say hello
    /// </summary>
    /// <returns></returns>
    public string SayHello() => jc.GetHello(str, str2);
}

/// <summary>
/// Interface for test class without default constructor
/// </summary>
public interface ITestNoDefaultConstructor
{
    /// <summary>
    /// say hello
    /// </summary>
    string SayHello();
}