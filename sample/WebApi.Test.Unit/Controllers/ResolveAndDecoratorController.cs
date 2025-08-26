using EasilyNET.AutoDependencyInjection;
using EasilyNET.AutoDependencyInjection.Core.Attributes;
using EasilyNET.Core.Misc;
using Microsoft.AspNetCore.Mvc;
using WebApi.Test.Unit.Decorators;

namespace WebApi.Test.Unit.Controllers;

/// <summary>
/// test controller without default constructor
/// </summary>
/// <param name="provider"></param>
/// <param name="jc"></param>
[Route("api/[controller]")]
[ApiController]
public class ResolveAndDecoratorController(IServiceProvider provider, [FromKeyedServices(nameof(FooService))] FooService jc) : ControllerBase
{
    /// <summary>
    /// Get
    /// </summary>
    /// <returns></returns>
    [HttpGet("TestNoDefaultCtor")]
    public string Get()
    {
        var resolver = provider.CreateResolver();
        var test = resolver.ResolveKeyed<IFooService>(nameof(TestNoDefaultConstructor), new NamedParameter("jc", jc), new NamedParameter("str", "EasilyNET"), new NamedParameter("str2", "World"));
        return test.SayHello();
    }

    /// <summary>
    /// 测试装饰器
    /// </summary>
    /// <returns></returns>
    [HttpGet("TestDecorator")]
    public string GetDecorator()
    {
        var resolver = provider.CreateResolver();
        var test = resolver.Resolve<TestNoDefaultDecorator>(new NamedParameter("jc", jc), new NamedParameter("str", "Rose"));
        return test.SayHello();
    }
}

/// <summary>
/// Test class without default constructor
/// </summary>
[DependencyInjection(ServiceLifetime.Singleton, ServiceKey = nameof(FooService))]
// ReSharper disable once ClassNeverInstantiated.Global
public sealed class FooService
{
    /// <summary>
    /// get hello
    /// </summary>
    /// <returns></returns>
    // ReSharper disable once MemberCanBeMadeStatic.Global
    public string GetHello(params IEnumerable<string> str) => $"Hello, {str.Join()}";
}

/// <summary>
/// Interface for test class without default constructor
/// </summary>
public interface IFooService
{
    /// <summary>
    /// say hello
    /// </summary>
    string SayHello();
}

/// <summary>
/// Test class without default constructor
/// </summary>
[DependencyInjection(ServiceLifetime.Transient, ServiceKey = nameof(TestNoDefaultConstructor))]
// ReSharper disable once ClassNeverInstantiated.Global
public sealed class TestNoDefaultConstructor(FooService jc, string str, string str2) : IFooService
{
    /// <summary>
    /// Say hello
    /// </summary>
    /// <returns></returns>
    public string SayHello() => jc.GetHello(str, str2);
}