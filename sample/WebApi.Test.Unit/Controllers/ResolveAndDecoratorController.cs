using EasilyNET.AutoDependencyInjection;
using EasilyNET.AutoDependencyInjection.Core.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Test.Unit.Controllers;

/// <summary>
/// test controller without default constructor
/// </summary>
/// <param name="provider"></param>
[Route("api/[controller]")]
[ApiController]
public class ResolveAndDecoratorController(IServiceProvider provider) : ControllerBase
{
    /// <summary>
    /// Get
    /// </summary>
    /// <returns></returns>
    [HttpGet("TestNoDefaultCtor")]
    public string Get()
    {
        var resolver = provider.CreateResolver();
        var service = resolver.Resolve<IFooService>(new NamedParameter("str", "Rose"));
        return service.SayHello();
    }
}

/// <summary>
/// Base service used for demonstration (keyed registration)
/// </summary>
[DependencyInjection(ServiceLifetime.Transient)]
// ReSharper disable once ClassNeverInstantiated.Global
public sealed class FooService(string str) : IFooService
{
    private string Name { get; } = str;

    /// <summary>
    /// SayHello
    /// </summary>
    /// <returns></returns>
    // ReSharper disable once MemberCanBeMadeStatic.Global
    public string SayHello() => $"Hello, {Name}";
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