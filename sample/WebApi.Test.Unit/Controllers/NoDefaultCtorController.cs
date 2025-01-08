using EasilyNET.AutoDependencyInjection.Core.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Test.Unit.Controllers;

/// <summary>
/// test controller without default constructor
/// </summary>
/// <param name="provider"></param>
[Route("api/[controller]")]
[ApiController]
public class NoDefaultCtorController(IServiceProvider provider) : ControllerBase
{
    /// <summary>
    /// Get
    /// </summary>
    /// <returns></returns>
    [HttpGet("TestNoDefaultCtor")]
    public string Get()
    {
        var test = provider.ResolveNamed<ITestNoDefaultCtor>("TestNoDefaultCtor", new Dictionary<string, object>
        {
            { "str", "Jack" }
        });
        return test.SayHello();
    }
}

/// <summary>
/// Test class without default constructor
/// </summary>
[DependencyInjection(ServiceLifetime.Singleton)]
public sealed class InjectClass
{
    /// <summary>
    /// get hello
    /// </summary>
    /// <returns></returns>
    public string GetHello(string str) => $"Hello, {str}";
}

/// <summary>
/// Test class without default constructor
/// </summary>
[DependencyInjection(ServiceLifetime.Transient, ServiceKey = "TestNoDefaultCtor")]
public sealed class TestNoDefaultCtor(InjectClass jc, string str) : ITestNoDefaultCtor
{
    /// <summary>
    /// String property
    /// </summary>
    public string Str { get; } = str;

    /// <summary>
    /// Say hello
    /// </summary>
    /// <returns></returns>
    public string SayHello() => jc.GetHello(Str);
}

/// <summary>
/// Interface for test class without default constructor
/// </summary>
public interface ITestNoDefaultCtor
{
    /// <summary>
    /// say hello
    /// </summary>
    string SayHello();
}